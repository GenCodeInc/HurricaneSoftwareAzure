using TTENET.TTEBusiness.Core.Models;

namespace TTENET.TTEBusiness.Core.Services;

public interface ITropicalStormsRepository
{
    Task<IReadOnlyList<GisDataItem>> GetGisDataAsync(bool activeOnly, CancellationToken cancellationToken);

    Task<IReadOnlyList<PointOfInterestItem>> GetPointsOfInterestAsync(CancellationToken cancellationToken);

    Task<IReadOnlyList<MobileTabGroupItem>> GetMobileTabGroupsAsync(int? regionType, int tabToShowOn, CancellationToken cancellationToken);

    Task<IReadOnlyList<SatelliteGroupItem>> GetSatelliteGroupsAsync(int? regionType, CancellationToken cancellationToken);

    Task<IReadOnlyList<StormSummaryItem>> GetStormNamesAsync(string region, bool activeOnly, CancellationToken cancellationToken);

    Task<StormDetailItem?> GetStormAsync(int stormId, bool withImageLinks, CancellationToken cancellationToken);

    Task<IReadOnlyList<StormDetailItem>> GetStormsAsync(
        string stormsToDownload,
        string region,
        bool withImageLinks,
        bool activeOnly,
        bool lastCoordinateOnly,
        bool forecastsToo,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<CoordinateItem>> GetCoordinatesAsync(int stormId, CancellationToken cancellationToken);

    Task<IReadOnlyList<ImageLinkItem>> GetImageLinksAsync(int? stormId, int imageLinkType, CancellationToken cancellationToken);

    Task<RegistrationRecordItem?> GetRegistrationAsync(string userId, CancellationToken cancellationToken);

    Task<RegistrationRecordItem?> GetRegistrationByEmailAsync(string lookup, CancellationToken cancellationToken);

    Task<IReadOnlyList<RegistrationRecordItem>> GetRegistrationsByEmailAlertAsync(string value, CancellationToken cancellationToken);

    Task<IReadOnlyList<RegistrationRecordItem>> GetRegistrationsByCellAlertAsync(string value, CancellationToken cancellationToken);

    Task UpdateRegistrationAsync(RegistrationRecordItem registration, CancellationToken cancellationToken);

    Task<IReadOnlyList<AlertRecordItem>> GetAlertsAsync(
        int? alertId,
        int? alertTypeId,
        string? value,
        bool? confirmed,
        int? applicationTypeId,
        string? externalKey,
        CancellationToken cancellationToken);

    Task<AlertRecordItem> CreateAlertAsync(int alertTypeId, string value, bool confirmed, int applicationTypeId, string externalKey, CancellationToken cancellationToken);

    Task DeleteAlertAsync(int alertId, CancellationToken cancellationToken);

    Task<long> IncrementApplicationHitCounterAsync(int applicationTypeId, CancellationToken cancellationToken);

    Task<bool> HasInvalidUserAsync(string userId, CancellationToken cancellationToken);

    Task<IReadOnlyList<AppLinkItem>> GetAppLinksAsync(
        string userId,
        string registrationNumber,
        string osBinaryTime,
        int numberOfTimesLoggedIn,
        string promo,
        int? appLinkType,
        int? regionType,
        bool? active,
        CancellationToken cancellationToken);

    Task<VersionInfoResult> GetVersionInfoAsync(int applicationType, string version, string promo, bool getZip, CancellationToken cancellationToken);
}