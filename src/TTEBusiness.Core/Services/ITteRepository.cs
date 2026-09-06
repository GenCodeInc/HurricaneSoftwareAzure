using TTENET.TTEBusiness.Core.Models;

namespace TTENET.TTEBusiness.Core.Services;

public interface ITteRepository
{
    Task<IReadOnlyList<AdvisoryRecord>> GetAdvisoriesAsync(int regionType, CancellationToken cancellationToken);

    Task<IReadOnlyList<string>> GetValidNamesAsync(CancellationToken cancellationToken);

    Task<PersistAdvisoryResult> PersistAdvisoryAsync(PersistAdvisoryRequest request, CancellationToken cancellationToken);

    Task<int> PersistForecastAsync(PersistForecastRequest request, CancellationToken cancellationToken);

    Task<int> DeactivateExpiredForecastsAsync(CancellationToken cancellationToken);

    Task<int> RemoveInactiveStormCenterItemsAsync(CancellationToken cancellationToken);

    Task<int> ReplacePointsOfInterestAsync(IReadOnlyList<PersistPointOfInterestRequest> requests, CancellationToken cancellationToken);
}
