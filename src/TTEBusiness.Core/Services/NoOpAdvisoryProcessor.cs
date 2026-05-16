using Microsoft.Extensions.Logging;
using TTENET.TTEBusiness.Core.Models;

namespace TTENET.TTEBusiness.Core.Services;

public sealed class NoOpAdvisoryProcessor(ILogger<NoOpAdvisoryProcessor> logger) : INhcAdvisoryProcessor
{
    public Task ProcessAsync(AdvisoryDocument document, CancellationToken cancellationToken)
    {
        logger.LogInformation(
            "Success. Source={SourceName}. Url={Url}. Type={AdvisoryType}. Size={ContentLength} chars. FetchedAtUtc={FetchedAtUtc}",
            document.Source.Name,
            document.Source.Url,
            document.AdvisoryType,
            document.Content.Length,
            document.FetchedAtUtc);

        return Task.CompletedTask;
    }
}