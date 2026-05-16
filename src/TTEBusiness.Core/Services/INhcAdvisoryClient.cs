using TTENET.TTEBusiness.Core.Models;

namespace TTENET.TTEBusiness.Core.Services;

public interface INhcAdvisoryClient
{
    Task<string> GetAdvisoryContentAsync(AdvisorySource source, CancellationToken cancellationToken);
}