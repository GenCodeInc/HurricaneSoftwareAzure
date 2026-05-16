using TropicalStorms.Api.Models;
using TTENET.TTEBusiness.Core.Models;

namespace TropicalStorms.Api.LegacyCompat;

internal static class LegacySoapMapper
{
    public const string ServiceNamespace = "http://www.hurricanesoftware.com/services";

    public static LegacyGisData[] Map(IEnumerable<GisDataItem> items) => items.Select(item => new LegacyGisData
    {
        Title = item.Title,
        Description = item.Description,
        URL = item.Url,
        Active = item.Active,
    }).ToArray();

    public static LegacyPointOfInterest[] Map(IEnumerable<PointOfInterestItem> items) => items.Select(item => new LegacyPointOfInterest
    {
        ID = item.Id,
        Latitude = item.Latitude,
        Longitude = item.Longitude,
        RelatedText = item.RelatedText,
        Type = (LegacyPointOfInteretType)Math.Clamp(item.Type, 0, 4),
    }).ToArray();

    public static LegacyMobileTabGroup[] Map(IEnumerable<MobileTabGroupItem> items) => items.Select(item => new LegacyMobileTabGroup
    {
        ID = item.ID,
        Header = item.Header,
        SubHeader = item.SubHeader,
        ThumbnailURL = item.ThumbnailURL,
        GroupRegion = (LegacyRegionTypeEnum)item.GroupRegion,
        TabToShowOn = item.TabToShowOn,
        ApplicationType = item.ApplicationType,
        MobileTabItems = item.MobileTabItems.Select(child => new LegacyMobileTabItem
        {
            ID = child.ID,
            MobileTabGroupID = child.MobileTabGroupID,
            ThumbnailURL = child.ThumbnailURL,
            URL = child.URL,
            Text = child.Text,
            ItemType = child.ItemType,
        }).ToArray(),
    }).ToArray();

    public static LegacySatelliteGroup[] Map(IEnumerable<SatelliteGroupItem> items) => items.Select(item => new LegacySatelliteGroup
    {
        ID = item.ID,
        Header = item.Header,
        SubHeader = item.SubHeader,
        ThumbnailURL = item.ThumbnailURL,
        GroupRegion = (LegacyRegionTypeEnum)item.GroupRegion,
        SatelliteItems = item.SatelliteItems.Select(child => new LegacySatelliteItem
        {
            ID = child.ID,
            SatelliteGroupID = child.SatelliteGroupID,
            ThumbnailURL = child.ThumbnailURL,
            URL = child.URL,
            Text = child.Text,
        }).ToArray(),
    }).ToArray();

    public static LegacyImageLink[] Map(IEnumerable<ImageLinkItem> items) => items.Select(item => new LegacyImageLink
    {
        ImageLinkID = item.ImageLinkId,
        ImageLinkType = item.ImageLinkType,
        StormID = item.StormId,
        URL = item.Url,
        DateUpated = item.DateUpdated,
    }).ToArray();

    public static LegacyCoordinate[] Map(IEnumerable<CoordinateItem> items, int stormNumber = 0) => items.Select(item => new LegacyCoordinate
    {
        StormID = item.StormId,
        StormNumber = stormNumber,
        AdvisoryNumber = item.AdvisoryNumber,
        Latitude = item.Latitude,
        Longitude = item.Longitude,
        WindSpeed = item.WindSpeed,
        WindSpeedKts = item.WindSpeed,
        SpeedTravel = item.SpeedTravel,
        SpeedTravelKts = item.SpeedTravel,
        Pressure = item.Pressure,
        Direction = item.Direction,
        DirectionDegrees = item.Direction * 225 / 10,
        DirectionStr = item.Heading,
        Heading = item.Heading,
        StormType = (LegacyStormTypeEnum)Math.Clamp(item.StormType, 0, 4),
        UTCOffset = item.UtcOffset,
        DateTime = item.CoordinateDate,
    }).ToArray();

    public static LegacyStorm[] Map(IEnumerable<StormDetailItem> items) => items.Select(Map).ToArray();

    public static LegacyStorm[] Map(IEnumerable<StormSummaryItem> items) => items.Select(item => new LegacyStorm
    {
        ID = item.Id,
        StormID = item.StormId,
        Name = item.Name,
        NameYear = item.NameYear,
        Region = item.Region,
        Year = item.Year,
        Active = item.Active,
        StormType = (LegacyStormTypeEnum)Math.Clamp(item.StormType, 0, 4),
        EmailAlertsSent = item.EmailAlertsSent,
        StormNumber = item.StormNumber,
        GISFiles = Array.Empty<LegacyGISFile>(),
        Coordinates = Array.Empty<LegacyCoordinate>(),
        ImageLinks = Array.Empty<LegacyImageLink>(),
        Details = string.Empty,
        ImageURL = string.Empty,
    }).ToArray();

    public static LegacyStorm Map(StormDetailItem item) => new()
    {
        ID = item.Id,
        StormID = item.StormId,
        Name = item.Name,
        NameYear = item.NameYear,
        Region = item.Region,
        Year = item.Year,
        Active = item.Active,
        StormType = (LegacyStormTypeEnum)Math.Clamp(item.StormType, 0, 4),
        IsModified = false,
        EmailAlertsSent = item.EmailAlertsSent,
        StormNumber = item.StormNumber,
        Details = item.Details,
        ImageURL = item.ImageUrl,
        GISFiles = Array.Empty<LegacyGISFile>(),
        Coordinates = Map(item.Coordinates, item.StormNumber),
        ImageLinks = Map(item.ImageLinks),
    };

    public static LegacyAppLink[] Map(IEnumerable<AppLinkItem> items) => items.Select(item => new LegacyAppLink
    {
        ID = item.Id,
        URL = item.Url,
        Region = item.RegionType switch
        {
            1 => LegacyRegionType.Atlantic,
            2 => LegacyRegionType.EasterPacific,
            _ => LegacyRegionType.All,
        },
        appLinksType = (LegacyAppLinksType)item.AppLinkType,
    }).ToArray();

    public static LegacyVersionInfo Map(VersionInfoResult item) => new()
    {
        SharewareLimit = item.SharewareLimit,
        RunningLatestVersion = item.RunningLatestVersion,
        RequiredUpdate = item.RequiredUpdate,
        LatestVersion = item.LatestVersion,
        DownloadLocation = item.DownloadLocation,
        ReturnMessage = Map(item.ReturnMessage),
    };

    public static LegacyReturnMessage Map(ReturnMessage item) => new()
    {
        MessageNumber = item.MessageNumber,
        Message = item.Message,
    };

    public static LegacyUser Map(UserResult item) => new()
    {
        VersionInfo = Map(item.VersionInfo),
        LoginMessageType = (LegacyAppLinksType)item.LoginMessageType,
        ShowLoginMessage = item.ShowLoginMessage,
        LoggedIn = item.LoggedIn,
        NeedToRegister = item.NeedToRegister,
        RunningLatestVersion = item.RunningLatestVersion,
        AppLinks = Map(item.AppLinks),
        ReturnMessage = Map(item.ReturnMessage),
    };

    public static LegacyGadget Map(GadgetResult item) => new()
    {
        Timer = item.Timer,
        VersionInfo = Map(item.VersionInfo),
        Storms = Map(item.Storms),
    };

    public static LegacyStormName[] MapLegacyNames(IEnumerable<LegacyStormNameItem> items) => items.Select(item => new LegacyStormName
    {
        NAME = item.NAME,
        YEAR = item.YEAR,
        REGION = item.REGION,
        ERROR_DESCRIPTION = item.ERROR_DESCRIPTION,
    }).ToArray();

    public static LegacyOverlay[] GetOverlays() =>
    [
        new LegacyOverlay
        {
            Description = "Current Radar US",
            Type = "IMAGE",
            TextColor = string.Empty,
            URL = "http://www.srh.noaa.gov/ridge/Conus/RadarImg/latest_radaronly.gif",
            HideLayers = string.Empty,
            LongitudeStart = -127.620375523875,
            LatitudeStart = 50.406626367301,
            LongitudeEnd = -66.5403755238754,
            LatitudeEnd = 21.706626367301,
        },
        new LegacyOverlay
        {
            Description = "GOES IR",
            Type = "WMS",
            TextColor = "White",
            URL = "http://nowcoast.noaa.gov/wms/com.esri.wms.Esrimap/obs?service=wms&version=1.1.1&request=GetMap&format=jpeg&SRS=EPSG:4269&Layers=world_countries,us_states_gen,RAS_GOES_I4",
            HideLayers = string.Empty,
        },
        new LegacyOverlay
        {
            Description = "GOES Visible",
            Type = "WMS",
            TextColor = "White",
            URL = "http://nowcoast.noaa.gov/wms/com.esri.wms.Esrimap/obs?service=wms&version=1.1.1&request=GetMap&format=png&transparent=true&SRS=EPSG:4269&Layers=world_rivers,major_roads,us_states_gen,world_countries,RAS_GOES",
            HideLayers = string.Empty,
        },
        new LegacyOverlay
        {
            Description = "Tornado Warnings",
            Type = "WMS",
            URL = "http://nowcoast.noaa.gov/wms/com.esri.wms.Esrimap/wwa?service=wms&version=1.1.1&request=GetMap&format=png&transparent=true&SRS=EPSG:4269&Layers=WARN_SHORT_TOR",
            HideLayers = string.Empty,
        },
        new LegacyOverlay
        {
            Description = "Severe Thunderstorm Warnings",
            Type = "WMS",
            URL = "http://nowcoast.noaa.gov/wms/com.esri.wms.Esrimap/wwa?service=wms&version=1.1.1&request=GetMap&format=png&transparent=true&SRS=EPSG:4269&Layers=WARN_SHORT_SVR",
            HideLayers = string.Empty,
        },
        new LegacyOverlay
        {
            Description = "Extreme Wind Warnings",
            Type = "WMS",
            URL = "http://nowcoast.noaa.gov/wms/com.esri.wms.Esrimap/wwa?service=wms&version=1.1.1&request=GetMap&format=png&transparent=true&SRS=EPSG:4269&Layers=WARN_SHORT_EWW",
            HideLayers = string.Empty,
        },
        new LegacyOverlay
        {
            Description = "Flood Warnings",
            Type = "WMS",
            URL = "http://nowcoast.noaa.gov/wms/com.esri.wms.Esrimap/wwa?service=wms&version=1.1.1&request=GetMap&format=png&transparent=true&SRS=EPSG:4269&Layers=WARN_SHORT_FLW",
            HideLayers = string.Empty,
        },
        new LegacyOverlay
        {
            Description = "Flash Flood Warnings",
            Type = "WMS",
            URL = "http://nowcoast.noaa.gov/wms/com.esri.wms.Esrimap/wwa?service=wms&version=1.1.1&request=GetMap&format=png&transparent=true&SRS=EPSG:4269&Layers=WARN_SHORT_FFW",
            HideLayers = string.Empty,
        },
        new LegacyOverlay
        {
            Description = "Special Marine Warnings",
            Type = "WMS",
            URL = "http://nowcoast.noaa.gov/wms/com.esri.wms.Esrimap/wwa?service=wms&version=1.1.1&request=GetMap&format=png&transparent=true&SRS=EPSG:4269&Layers=WARN_SHORT_SMW",
            HideLayers = string.Empty,
        },
    ];
}