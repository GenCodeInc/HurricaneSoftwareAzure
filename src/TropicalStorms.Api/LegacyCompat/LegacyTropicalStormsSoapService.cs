using System.Data;
using TropicalStorms.Api.Services;
using TTENET.TTEBusiness.Core.Services;

namespace TropicalStorms.Api.LegacyCompat;

public sealed class LegacyTropicalStormsSoapService(ITropicalStormsRepository repository, ITropicalStormsFacade facade) : ILegacyTropicalStormsSoapService
{
    public string HelloWorld() => "Hello World";

    public LegacyGisData[] GetGISData() => LegacySoapMapper.Map(repository.GetGisDataAsync(true, CancellationToken.None).GetAwaiter().GetResult());

    public LegacyOverlay[] GetStormOverlays() => LegacySoapMapper.GetOverlays();

    public LegacySatelliteGroup[] MobileSatelliteTabAndroid(int appTypeID, int regionType)
        => LegacySoapMapper.Map(facade.GetMobileSatelliteTabAsync(appTypeID, regionType, CancellationToken.None).GetAwaiter().GetResult());

    public LegacyMobileTabGroup[] MobileTabs(int appTypeID, int regionType, int tabToShowOn)
        => LegacySoapMapper.Map(facade.GetMobileTabsAsync(appTypeID, regionType, tabToShowOn, CancellationToken.None).GetAwaiter().GetResult());

    public LegacySatelliteGroup[] MobileSatelliteTab(LegacyApplicationTypeEnum appTypeID, LegacyRegionTypeEnum regionType)
        => LegacySoapMapper.Map(facade.GetMobileSatelliteTabAsync((int)appTypeID, (int)regionType, CancellationToken.None).GetAwaiter().GetResult());

    public string CreateAlerts(int alertTypeID, string email, int appTypeID, string deviceID, int regionType)
        => facade.CreateAlertsAsync(alertTypeID, email, appTypeID, deviceID, regionType, CancellationToken.None).GetAwaiter().GetResult();

    public string CreateAlert(LegacyAlertTypeEnum alertTypeID, string email, LegacyApplicationTypeEnum appTypeID, string deviceID, LegacyRegionTypeEnum regionType)
        => facade.CreateAlertAsync((int)alertTypeID, email, (int)appTypeID, deviceID, (int)regionType, CancellationToken.None).GetAwaiter().GetResult();

    public string RemoveAlert(string value, LegacyRegionTypeEnum region)
        => facade.RemoveAlertAsync(value, (int)region, CancellationToken.None).GetAwaiter().GetResult();

    public LegacyPointOfInterest[] PointsOfInterest()
        => LegacySoapMapper.Map(repository.GetPointsOfInterestAsync(CancellationToken.None).GetAwaiter().GetResult());

    public string GetRegCode(string userID) => facade.GetRegCode(userID);

    public LegacyReturnMessage RetrieveRegistration(string Email)
        => LegacySoapMapper.Map(facade.RetrieveRegistrationAsync(Email, CancellationToken.None).GetAwaiter().GetResult());

    public LegacyUser LoginUser(string userID, string registrationNumber, string osBinaryTime, int numberOfTimesLoggedIn, bool isRegistered, string TTEVersion, string promo)
        => LegacySoapMapper.Map(facade.LoginUserAsync(userID, registrationNumber, osBinaryTime, numberOfTimesLoggedIn, isRegistered, TTEVersion, promo, CancellationToken.None).GetAwaiter().GetResult());

    public LegacyAppLink[] AppLinksAndroid(string userID, string registrationNumber, string osBinaryTime, int numberOfTimesLoggedIn, string promo, int appLinkType, int regionType)
        => LegacySoapMapper.Map(repository.GetAppLinksAsync(userID, registrationNumber, osBinaryTime, numberOfTimesLoggedIn, promo, appLinkType, regionType, null, CancellationToken.None).GetAwaiter().GetResult());

    public LegacyAppLink[] AppLinks(string userID, string registrationNumber, string osBinaryTime, int numberOfTimesLoggedIn, string promo, LegacyAppLinksType appLinkType, LegacyRegionTypeEnum regionType)
    {
        int? appLinkTypeFilter = appLinkType == LegacyAppLinksType.All ? null : (int)appLinkType;
        int? regionTypeFilter = regionType == LegacyRegionTypeEnum.Unknown ? null : (int)regionType;
        return LegacySoapMapper.Map(repository.GetAppLinksAsync(userID, registrationNumber, osBinaryTime, numberOfTimesLoggedIn, promo, appLinkTypeFilter, regionTypeFilter, null, CancellationToken.None).GetAwaiter().GetResult());
    }

    public LegacyVersionInfo VersionCheck(int ApplicationType, string version, string promo, bool getzip)
        => LegacySoapMapper.Map(repository.GetVersionInfoAsync(ApplicationType, version, promo, getzip, CancellationToken.None).GetAwaiter().GetResult());

    public LegacyReturnMessage ValidateUser(string userID, string registrationNumber)
        => LegacySoapMapper.Map(facade.ValidateUserAsync(userID, registrationNumber, CancellationToken.None).GetAwaiter().GetResult());

    public DateTime GetTimeStamp(string region) => DateTime.Now;

    public LegacyStorm[] StormNames(string username, string password, string region, bool activeOnly)
        => LegacySoapMapper.Map(repository.GetStormNamesAsync(region, activeOnly, CancellationToken.None).GetAwaiter().GetResult());

    public double GetDistanceBetweenPoints(double startLat, double startLong, double endLat, double endLon)
    {
        const double statuteMultiplier = 1.150757575758d;
        var nauticalMiles = LatLon2NauticalMiles(startLat, startLong, endLat, endLon);
        return nauticalMiles * statuteMultiplier;
    }

    public LegacyStorm GetStorm(string username, string password, int stormID, bool withImageLinks)
    {
        var storm = repository.GetStormAsync(stormID, withImageLinks, CancellationToken.None).GetAwaiter().GetResult();
        return storm is null ? new LegacyStorm() : LegacySoapMapper.Map(storm);
    }

    public LegacyGadget GetGadget(string region, LegacyGadgetType GadgetType, string Version)
        => LegacySoapMapper.Map(facade.GetGadgetAsync(region, (int)GadgetType, Version, CancellationToken.None).GetAwaiter().GetResult());

    public LegacyStorm[] Storms(string username, string password, string StormsToDownload, string region, bool withImageLinks, bool activeOnly, bool lastCoordinateOnly, bool omitForecastsToo)
        => LegacySoapMapper.Map(facade.GetStormsAsync(StormsToDownload, region, withImageLinks, activeOnly, lastCoordinateOnly, omitForecastsToo, CancellationToken.None).GetAwaiter().GetResult());

    public LegacyCoordinate[] GetCoordinates(string username, string password, int StormID)
        => LegacySoapMapper.Map(repository.GetCoordinatesAsync(StormID, CancellationToken.None).GetAwaiter().GetResult());

    public LegacyImageLink[] ImageLinks(string username, string password, int stormID, LegacyImageLinkType imageLinkType)
        => LegacySoapMapper.Map(repository.GetImageLinksAsync(stormID == 0 ? null : stormID, (int)imageLinkType, CancellationToken.None).GetAwaiter().GetResult());

    public LegacyStormName[] GetStormNames(string username, string password, string region)
        => LegacySoapMapper.MapLegacyNames(facade.GetLegacyStormNamesAsync(username, password, region, CancellationToken.None).GetAwaiter().GetResult());

    public DataSet GetStormsDataset(string username, string password, string StormsToDownload, string region)
        => facade.GetStormsDatasetAsync(username, password, StormsToDownload, region, CancellationToken.None).GetAwaiter().GetResult();

    private static double LatLon2NauticalMiles(double latitudeStart, double longitudeStart, double latitudeEnd, double longitudeEnd)
    {
        const double halfPi = Math.PI / 2d;
        var degreesToRadians = Math.PI / 180d;
        var answer = ArcHaversine(
            Haversine((halfPi - latitudeEnd * degreesToRadians) - (halfPi - latitudeStart * degreesToRadians)) +
            Math.Sin(halfPi - latitudeEnd * degreesToRadians) * Math.Sin(halfPi - latitudeStart * degreesToRadians) *
            Haversine((longitudeEnd - longitudeStart) * degreesToRadians)) / degreesToRadians;
        return 60d * answer;
    }

    private static double Haversine(double value) => (1d - Math.Cos(value)) / 2d;

    private static double ArcHaversine(double value) => Math.Acos(1d - 2d * value);
}