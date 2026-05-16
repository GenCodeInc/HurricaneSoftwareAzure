using System.Data;
using TTENET.TTEBusiness.Core.Models;

namespace TropicalStorms.Api.Services;

public interface ITropicalStormsFacade
{
    Task<IReadOnlyList<SatelliteGroupItem>> GetMobileSatelliteTabAsync(int appTypeId, int regionType, CancellationToken cancellationToken);

    Task<IReadOnlyList<MobileTabGroupItem>> GetMobileTabsAsync(int appTypeId, int regionType, int tabToShowOn, CancellationToken cancellationToken);

    Task<string> CreateAlertsAsync(int alertTypeId, string email, int appTypeId, string deviceId, int regionType, CancellationToken cancellationToken);

    Task<string> CreateAlertAsync(int alertTypeId, string email, int appTypeId, string deviceId, int regionType, CancellationToken cancellationToken);

    Task<string> RemoveAlertAsync(string value, int regionType, CancellationToken cancellationToken);

    string GetRegCode(string userId);

    Task<ReturnMessage> RetrieveRegistrationAsync(string email, CancellationToken cancellationToken);

    Task<UserResult> LoginUserAsync(string userId, string registrationNumber, string osBinaryTime, int numberOfTimesLoggedIn, bool isRegistered, string version, string promo, CancellationToken cancellationToken);

    Task<ReturnMessage> ValidateUserAsync(string userId, string registrationNumber, CancellationToken cancellationToken);

    Task<GadgetResult> GetGadgetAsync(string region, int gadgetType, string version, CancellationToken cancellationToken);

    Task<IReadOnlyList<StormDetailItem>> GetStormsAsync(string stormsToDownload, string region, bool withImageLinks, bool activeOnly, bool lastCoordinateOnly, bool omitForecastsToo, CancellationToken cancellationToken);

    Task<IReadOnlyList<LegacyStormNameItem>> GetLegacyStormNamesAsync(string username, string password, string region, CancellationToken cancellationToken);

    Task<DataSet> GetStormsDatasetAsync(string username, string password, string stormsToDownload, string region, CancellationToken cancellationToken);
}
