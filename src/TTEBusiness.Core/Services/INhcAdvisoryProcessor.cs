using TTENET.TTEBusiness.Core.Models;

namespace TTENET.TTEBusiness.Core.Services;

public interface INhcAdvisoryProcessor
{
    Task ProcessAsync(AdvisoryDocument document, CancellationToken cancellationToken);
}