using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using TTENET.TTEBusiness.Core.Services;

namespace NHCParser.Function.Functions;

public sealed class NhcParserTimerFunction(INhcParserRunner parserRunner, ILogger<NhcParserTimerFunction> logger)
{
    [Function("NHCParserTimer")]
    public async Task RunAsync([TimerTrigger("%NHC_TIMER_SCHEDULE%")]
        TimerInfo timerInfo,
        CancellationToken cancellationToken)
    {
        logger.LogInformation("NHC parser timer started at {StartedAtUtc}. IsPastDue={IsPastDue}", DateTimeOffset.UtcNow, timerInfo.IsPastDue);
        await parserRunner.RunAsync(cancellationToken).ConfigureAwait(false);
        logger.LogInformation("NHC parser timer completed at {CompletedAtUtc}", DateTimeOffset.UtcNow);
    }
}