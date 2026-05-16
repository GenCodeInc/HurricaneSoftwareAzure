using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TTENET.TTEBusiness.Core.Models;

namespace TTENET.TTEBusiness.Core.Services;

public sealed class NhcParserRunner(
    IOptions<NHCParserOptions> options,
    INhcAdvisoryClient advisoryClient,
    NhcAdvisoryClassifier advisoryClassifier,
    INhcAdvisoryProcessor advisoryProcessor,
    ITteRepository tteRepository,
    ILogger<NhcParserRunner> logger) : INhcParserRunner
{
    private static readonly SemaphoreSlim RunLock = new(1, 1);
    private int hasProbedDatabase;

    public async Task RunAsync(CancellationToken cancellationToken)
    {
        if (!await RunLock.WaitAsync(0, cancellationToken).ConfigureAwait(false))
        {
            logger.LogWarning("NHC parser run skipped because another run is already in progress.");
            return;
        }

        try
        {
        var parserOptions = options.Value;

        await ProbeDatabaseAsync(parserOptions, cancellationToken).ConfigureAwait(false);

        if (parserOptions.Sources.Count == 0)
        {
            logger.LogWarning("No NHC advisory sources are configured.");
            return;
        }

        foreach (var source in parserOptions.Sources)
        {
            string content;

            try
            {
                content = await advisoryClient.GetAdvisoryContentAsync(source, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex) when (!cancellationToken.IsCancellationRequested)
            {
                if (source.ReportFailures)
                {
                    logger.LogError(ex, "NHC source {SourceName} download failed for URL {Url}.", source.Name, source.Url);
                }
                else
                {
                    logger.LogWarning(ex, "NHC source {SourceName} download failed for URL {Url}, but failure reporting is disabled for this source.", source.Name, source.Url);
                }

                continue;
            }

            try
            {
                var advisoryType = advisoryClassifier.Classify(content);

                var document = new AdvisoryDocument
                {
                    Source = source,
                    Content = content,
                    AdvisoryType = advisoryType,
                    FetchedAtUtc = DateTimeOffset.UtcNow,
                };

                await advisoryProcessor.ProcessAsync(document, cancellationToken).ConfigureAwait(false);

                if (parserOptions.LogSuccessfulRuns)
                {
                    logger.LogInformation("NHC source {SourceName} completed successfully with advisory type {AdvisoryType}.", source.Name, advisoryType);
                }
            }
            catch (Exception ex) when (!cancellationToken.IsCancellationRequested)
            {
                if (source.ReportFailures)
                {
                    logger.LogError(ex, "NHC source {SourceName} processing failed after download for URL {Url}.", source.Name, source.Url);
                }
                else
                {
                    logger.LogWarning(ex, "NHC source {SourceName} processing failed after download for URL {Url}, but failure reporting is disabled for this source.", source.Name, source.Url);
                }
            }
        }

        try
        {
            var expiredForecastsDeactivated = await tteRepository.DeactivateExpiredForecastsAsync(cancellationToken).ConfigureAwait(false);
            if (expiredForecastsDeactivated > 0)
            {
                logger.LogInformation("Deactivated {Count} expired forecast storms.", expiredForecastsDeactivated);
            }
        }
        catch (Exception ex) when (!cancellationToken.IsCancellationRequested)
        {
            logger.LogError(ex, "Failed to deactivate expired forecast storms.");
        }
        }
        finally
        {
            RunLock.Release();
        }
    }

    private async Task ProbeDatabaseAsync(NHCParserOptions parserOptions, CancellationToken cancellationToken)
    {
        if (!parserOptions.ProbeDatabaseOnStartup)
        {
            return;
        }

        if (Interlocked.Exchange(ref hasProbedDatabase, 1) == 1)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(parserOptions.SqlConnectionString))
        {
            logger.LogWarning("Advisory database probe is enabled, but no SQL connection string is configured.");
            return;
        }

        var regions = parserOptions.DatabaseProbeRegions.Count == 0 ? new[] { 1, 2 } : parserOptions.DatabaseProbeRegions.Distinct().ToArray();
        foreach (var regionType in regions)
        {
            try
            {
                var advisories = await tteRepository.GetAdvisoriesAsync(regionType, cancellationToken).ConfigureAwait(false);
                logger.LogInformation("Advisory database probe loaded {Count} rows from dbo.msp_Advisory_Get for RegionType {RegionType}.", advisories.Count, regionType);

                foreach (var advisory in advisories.Where(static advisory => !string.IsNullOrWhiteSpace(advisory.URL)).Take(Math.Max(1, parserOptions.DatabaseProbeMaxUrlsToLog)))
                {
                    logger.LogInformation(
                        "Advisory SP sample. RegionType={RegionType}. AdvisoryIndex={AdvisoryIndex}. Title={Title}. Url={Url}",
                        regionType,
                        advisory.AdvisoryIndex,
                        advisory.Title,
                        advisory.URL);
                }
            }
            catch (Exception ex) when (!cancellationToken.IsCancellationRequested)
            {
                logger.LogError(ex, "Advisory database probe failed for RegionType {RegionType}.", regionType);
            }
        }
    }
}