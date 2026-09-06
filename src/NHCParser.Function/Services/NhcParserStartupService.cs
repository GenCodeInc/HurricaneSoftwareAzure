using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TTENET.TTEBusiness.Core.Models;
using TTENET.TTEBusiness.Core.Services;

namespace NHCParser.Function.Services;

public sealed class NhcParserStartupService(
    IOptions<NHCParserOptions> options,
    INhcParserRunner parserRunner,
    ILogger<NhcParserStartupService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!options.Value.RunOnStartup)
        {
            logger.LogInformation("NHC parser startup run is disabled.");
            return;
        }

        try
        {
            logger.LogInformation("NHC parser startup run started at {StartedAtUtc}.", DateTimeOffset.UtcNow);
            await parserRunner.RunAsync(stoppingToken).ConfigureAwait(false);
            logger.LogInformation("NHC parser startup run completed at {CompletedAtUtc}.", DateTimeOffset.UtcNow);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            logger.LogInformation("NHC parser startup run was canceled because the Function host is stopping.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "NHC parser startup run failed.");
        }
    }
}
