using Microsoft.AspNetCore.Mvc;
using TropicalStorms.Api.Models;
using TropicalStorms.Api.Services;
using TTENET.TTEBusiness.Core.Models;
using TTENET.TTEBusiness.Core.Services;
using System.Data;

namespace TropicalStorms.Api.Controllers;

[ApiController]
[Route("api/tropical-storms/[action]")]
public sealed class TropicalStormsController(ITropicalStormsRepository repository, ITropicalStormsFacade facade) : ControllerBase
{
    [HttpGet]
    public ActionResult<string> HelloWorld() => "Hello World";

    [HttpGet]
    public ActionResult<DateTime> GetTimeStamp(string region) => DateTime.Now;

    [HttpGet]
    public ActionResult<double> GetDistanceBetweenPoints(double startLat, double startLong, double endLat, double endLon)
        => LatLon2Miles(startLat, startLong, endLat, endLon);

    [HttpGet]
    public ActionResult<IReadOnlyList<OverlayDto>> GetStormOverlays() => Ok(new List<OverlayDto>
    {
        new()
        {
            Description = "Current Radar US",
            Type = "IMAGE",
            TextColor = string.Empty,
            Url = "http://www.srh.noaa.gov/ridge/Conus/RadarImg/latest_radaronly.gif",
            HideLayers = string.Empty,
            LongitudeStart = -127.620375523875,
            LatitudeStart = 50.406626367301,
            LongitudeEnd = -66.5403755238754,
            LatitudeEnd = 21.706626367301,
        },
        new()
        {
            Description = "GOES IR",
            Type = "WMS",
            TextColor = "White",
            Url = "http://nowcoast.noaa.gov/wms/com.esri.wms.Esrimap/obs?service=wms&version=1.1.1&request=GetMap&format=jpeg&SRS=EPSG:4269&Layers=world_countries,us_states_gen,RAS_GOES_I4",
            HideLayers = string.Empty,
        },
        new()
        {
            Description = "GOES Visible",
            Type = "WMS",
            TextColor = "White",
            Url = "http://nowcoast.noaa.gov/wms/com.esri.wms.Esrimap/obs?service=wms&version=1.1.1&request=GetMap&format=png&transparent=true&SRS=EPSG:4269&Layers=world_rivers,major_roads,us_states_gen,world_countries,RAS_GOES",
            HideLayers = string.Empty,
        },
        new()
        {
            Description = "Tornado Warnings",
            Type = "WMS",
            Url = "http://nowcoast.noaa.gov/wms/com.esri.wms.Esrimap/wwa?service=wms&version=1.1.1&request=GetMap&format=png&transparent=true&SRS=EPSG:4269&Layers=WARN_SHORT_TOR",
            HideLayers = string.Empty,
        },
        new()
        {
            Description = "Severe Thunderstorm Warnings",
            Type = "WMS",
            Url = "http://nowcoast.noaa.gov/wms/com.esri.wms.Esrimap/wwa?service=wms&version=1.1.1&request=GetMap&format=png&transparent=true&SRS=EPSG:4269&Layers=WARN_SHORT_SVR",
            HideLayers = string.Empty,
        },
        new()
        {
            Description = "Extreme Wind Warnings",
            Type = "WMS",
            Url = "http://nowcoast.noaa.gov/wms/com.esri.wms.Esrimap/wwa?service=wms&version=1.1.1&request=GetMap&format=png&transparent=true&SRS=EPSG:4269&Layers=WARN_SHORT_EWW",
            HideLayers = string.Empty,
        },
        new()
        {
            Description = "Flood Warnings",
            Type = "WMS",
            Url = "http://nowcoast.noaa.gov/wms/com.esri.wms.Esrimap/wwa?service=wms&version=1.1.1&request=GetMap&format=png&transparent=true&SRS=EPSG:4269&Layers=WARN_SHORT_FLW",
            HideLayers = string.Empty,
        },
        new()
        {
            Description = "Flash Flood Warnings",
            Type = "WMS",
            Url = "http://nowcoast.noaa.gov/wms/com.esri.wms.Esrimap/wwa?service=wms&version=1.1.1&request=GetMap&format=png&transparent=true&SRS=EPSG:4269&Layers=WARN_SHORT_FFW",
            HideLayers = string.Empty,
        },
        new()
        {
            Description = "Special Marine Warnings",
            Type = "WMS",
            Url = "http://nowcoast.noaa.gov/wms/com.esri.wms.Esrimap/wwa?service=wms&version=1.1.1&request=GetMap&format=png&transparent=true&SRS=EPSG:4269&Layers=WARN_SHORT_SMW",
            HideLayers = string.Empty,
        },
    });

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<GisDataItem>>> GetGISData(CancellationToken cancellationToken)
        => Ok(await repository.GetGisDataAsync(activeOnly: true, cancellationToken).ConfigureAwait(false));

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<SatelliteGroupItem>>> MobileSatelliteTabAndroid(int appTypeID, int regionType, CancellationToken cancellationToken)
        => Ok(await facade.GetMobileSatelliteTabAsync(appTypeID, regionType, cancellationToken).ConfigureAwait(false));

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<MobileTabGroupItem>>> MobileTabs(int appTypeID, int regionType, int tabToShowOn, CancellationToken cancellationToken)
        => Ok(await facade.GetMobileTabsAsync(appTypeID, regionType, tabToShowOn, cancellationToken).ConfigureAwait(false));

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<SatelliteGroupItem>>> MobileSatelliteTab(int appTypeID, int regionType, CancellationToken cancellationToken)
        => Ok(await facade.GetMobileSatelliteTabAsync(appTypeID, regionType, cancellationToken).ConfigureAwait(false));

    [HttpGet]
    public async Task<ActionResult<string>> CreateAlerts(int alertTypeID, string email, int appTypeID, string deviceID, int regionType, CancellationToken cancellationToken)
        => Ok(await facade.CreateAlertsAsync(alertTypeID, email, appTypeID, deviceID, regionType, cancellationToken).ConfigureAwait(false));

    [HttpGet]
    public async Task<ActionResult<string>> CreateAlert(int alertTypeID, string email, int appTypeID, string deviceID, int regionType, CancellationToken cancellationToken)
        => Ok(await facade.CreateAlertAsync(alertTypeID, email, appTypeID, deviceID, regionType, cancellationToken).ConfigureAwait(false));

    [HttpGet]
    public async Task<ActionResult<string>> RemoveAlert(string value, int region, CancellationToken cancellationToken)
        => Ok(await facade.RemoveAlertAsync(value, region, cancellationToken).ConfigureAwait(false));

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<PointOfInterestItem>>> PointsOfInterest(CancellationToken cancellationToken)
        => Ok(await repository.GetPointsOfInterestAsync(cancellationToken).ConfigureAwait(false));

    [HttpGet]
    public ActionResult<string> GetRegCode(string userID)
        => Ok(facade.GetRegCode(userID));

    [HttpGet]
    public async Task<ActionResult<ReturnMessage>> RetrieveRegistration(string email, CancellationToken cancellationToken)
        => Ok(await facade.RetrieveRegistrationAsync(email, cancellationToken).ConfigureAwait(false));

    [HttpGet]
    public async Task<ActionResult<UserResult>> LoginUser(string userID, string registrationNumber, string osBinaryTime, int numberOfTimesLoggedIn, bool isRegistered, string TTEVersion, string promo, CancellationToken cancellationToken)
        => Ok(await facade.LoginUserAsync(userID, registrationNumber, osBinaryTime, numberOfTimesLoggedIn, isRegistered, TTEVersion, promo, cancellationToken).ConfigureAwait(false));

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<StormSummaryItem>>> StormNames(string username, string password, string region, bool activeOnly, CancellationToken cancellationToken)
        => Ok(await repository.GetStormNamesAsync(region, activeOnly, cancellationToken).ConfigureAwait(false));

    [HttpGet]
    public async Task<ActionResult<ReturnMessage>> ValidateUser(string userID, string registrationNumber, CancellationToken cancellationToken)
        => Ok(await facade.ValidateUserAsync(userID, registrationNumber, cancellationToken).ConfigureAwait(false));

    [HttpGet]
    public async Task<ActionResult<StormDetailItem>> GetStorm(string username, string password, int stormID, bool withImageLinks = false, CancellationToken cancellationToken = default)
    {
        var storm = await repository.GetStormAsync(stormID, withImageLinks, cancellationToken).ConfigureAwait(false);
        return storm is null ? NotFound() : Ok(storm);
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<CoordinateItem>>> GetCoordinates(string username, string password, int stormID, CancellationToken cancellationToken)
        => Ok(await repository.GetCoordinatesAsync(stormID, cancellationToken).ConfigureAwait(false));

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ImageLinkItem>>> ImageLinks(string username, string password, int stormID, int imageLinkType, CancellationToken cancellationToken)
        => Ok(await repository.GetImageLinksAsync(stormID == 0 ? null : stormID, imageLinkType, cancellationToken).ConfigureAwait(false));

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<AppLinkItem>>> AppLinksAndroid(
        string userID,
        string registrationNumber,
        string osBinaryTime,
        int numberOfTimesLoggedIn,
        string promo,
        int appLinkType,
        int regionType,
        CancellationToken cancellationToken)
        => Ok(await repository.GetAppLinksAsync(userID, registrationNumber, osBinaryTime, numberOfTimesLoggedIn, promo, appLinkType, regionType, null, cancellationToken).ConfigureAwait(false));

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<AppLinkItem>>> AppLinks(
        string userID,
        string registrationNumber,
        string osBinaryTime,
        int numberOfTimesLoggedIn,
        string promo,
        int appLinkType,
        int regionType,
        CancellationToken cancellationToken)
    {
        int? appLinkTypeFilter = appLinkType == 0 ? null : appLinkType;
        int? regionTypeFilter = regionType == 0 ? null : regionType;

        return Ok(await repository.GetAppLinksAsync(
            userID,
            registrationNumber,
            osBinaryTime,
            numberOfTimesLoggedIn,
            promo,
            appLinkTypeFilter,
            regionTypeFilter,
            null,
            cancellationToken).ConfigureAwait(false));
    }

    [HttpGet]
    public async Task<ActionResult<VersionInfoResult>> VersionCheck(int applicationType, string version, string promo, bool getzip = false, CancellationToken cancellationToken = default)
        => Ok(await repository.GetVersionInfoAsync(applicationType, version, promo, getzip, cancellationToken).ConfigureAwait(false));

    [HttpGet]
    public async Task<ActionResult<GadgetResult>> GetGadget(string region, int gadgetType, string version, CancellationToken cancellationToken)
        => Ok(await facade.GetGadgetAsync(region, gadgetType, version, cancellationToken).ConfigureAwait(false));

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<StormDetailItem>>> Storms(
        string username,
        string password,
        string stormsToDownload,
        string region,
        bool withImageLinks = false,
        bool activeOnly = false,
        bool lastCoordinateOnly = false,
        bool omitForecastsToo = false,
        CancellationToken cancellationToken = default)
        => Ok(await facade.GetStormsAsync(stormsToDownload, region, withImageLinks, activeOnly, lastCoordinateOnly, omitForecastsToo, cancellationToken).ConfigureAwait(false));

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<LegacyStormNameItem>>> GetStormNames(string username, string password, string region, CancellationToken cancellationToken)
        => Ok(await facade.GetLegacyStormNamesAsync(username, password, region, cancellationToken).ConfigureAwait(false));

    [HttpGet]
    public async Task<ActionResult<DataSet>> GetStormsDataset(string username, string password, string stormsToDownload, string region, CancellationToken cancellationToken)
        => Ok(await facade.GetStormsDatasetAsync(username, password, stormsToDownload, region, cancellationToken).ConfigureAwait(false));

    private static double LatLon2Miles(double latitudeStart, double longitudeStart, double latitudeEnd, double longitudeEnd)
    {
        var nauticalMiles = LatLon2NauticalMiles(latitudeStart, longitudeStart, latitudeEnd, longitudeEnd);
        return nauticalMiles * 1.150757575758d;
    }

    private static double LatLon2NauticalMiles(double latitudeStart, double longitudeStart, double latitudeEnd, double longitudeEnd)
    {
        const double pi = Math.PI;
        var degreesToRadians = pi / 180d;
        var halfPi = pi / 2d;

        var answer = ArcHaversine(
            Haversine((halfPi - latitudeEnd * degreesToRadians) - (halfPi - latitudeStart * degreesToRadians)) +
            Math.Sin(halfPi - latitudeEnd * degreesToRadians) * Math.Sin(halfPi - latitudeStart * degreesToRadians) *
            Haversine((longitudeEnd - longitudeStart) * degreesToRadians)) / degreesToRadians;

        return 60d * answer;
    }

    private static double Haversine(double value) => (1d - Math.Cos(value)) / 2d;

    private static double ArcHaversine(double value) => Math.Acos(1d - 2d * value);
}