namespace TTENET.TTEBusiness.Core.Services;

public interface INhcParserRunner
{
    Task RunAsync(CancellationToken cancellationToken);
}