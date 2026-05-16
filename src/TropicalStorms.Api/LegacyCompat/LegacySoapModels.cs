using System.Xml.Serialization;

namespace TropicalStorms.Api.LegacyCompat;

[XmlType(TypeName = "BaseBusiness", Namespace = LegacySoapMapper.ServiceNamespace)]
public class LegacyBaseBusiness
{
}

[XmlType(TypeName = "GISData", Namespace = LegacySoapMapper.ServiceNamespace)]
public class LegacyGisData : LegacyBaseBusiness
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string URL { get; set; } = string.Empty;
    public bool Active { get; set; }
}

[XmlType(TypeName = "STORM", Namespace = LegacySoapMapper.ServiceNamespace)]
public class LegacyStormName
{
    public string NAME { get; set; } = string.Empty;
    public string YEAR { get; set; } = string.Empty;
    public string REGION { get; set; } = string.Empty;
    public string ERROR_DESCRIPTION { get; set; } = string.Empty;
}

[XmlType(TypeName = "PointOfInterest", Namespace = LegacySoapMapper.ServiceNamespace)]
public class LegacyPointOfInterest
{
    public int ID { get; set; }
    public float Latitude { get; set; }
    public float Longitude { get; set; }
    public string RelatedText { get; set; } = string.Empty;
    public LegacyPointOfInteretType Type { get; set; }
}

[XmlType(TypeName = "PointOfInteretType", Namespace = LegacySoapMapper.ServiceNamespace)]
public enum LegacyPointOfInteretType
{
    ReconFlight,
    TropicalWave,
    PointOfInterest,
    Investigation,
    AreaOfInterest,
}

[XmlType(TypeName = "MobileTabItem", Namespace = LegacySoapMapper.ServiceNamespace)]
public class LegacyMobileTabItem
{
    public int ID { get; set; }
    public int MobileTabGroupID { get; set; }
    public string ThumbnailURL { get; set; } = string.Empty;
    public string URL { get; set; } = string.Empty;
    public string Text { get; set; } = string.Empty;
    public int ItemType { get; set; }
}

[XmlType(TypeName = "MobileTabGroup", Namespace = LegacySoapMapper.ServiceNamespace)]
public class LegacyMobileTabGroup
{
    public int ID { get; set; }
    public string Header { get; set; } = string.Empty;
    public string SubHeader { get; set; } = string.Empty;
    public string ThumbnailURL { get; set; } = string.Empty;
    public LegacyRegionTypeEnum GroupRegion { get; set; }
    public LegacyMobileTabItem[] MobileTabItems { get; set; } = Array.Empty<LegacyMobileTabItem>();
    public int TabToShowOn { get; set; }
    public int ApplicationType { get; set; }
}

 [XmlType(TypeName = "RegionTypeEnum", Namespace = LegacySoapMapper.ServiceNamespace)]
public enum LegacyRegionTypeEnum
{
    Unknown,
    Atlantic,
    EasternPacific,
    NorthWestPacific,
    SouthWestPacific,
    SouthIndian,
    NorthIndian,
}

[XmlType(TypeName = "SatelliteItem", Namespace = LegacySoapMapper.ServiceNamespace)]
public class LegacySatelliteItem
{
    public int ID { get; set; }
    public int SatelliteGroupID { get; set; }
    public string ThumbnailURL { get; set; } = string.Empty;
    public string URL { get; set; } = string.Empty;
    public string Text { get; set; } = string.Empty;
}

[XmlType(TypeName = "SatelliteGroup", Namespace = LegacySoapMapper.ServiceNamespace)]
public class LegacySatelliteGroup
{
    public int ID { get; set; }
    public string Header { get; set; } = string.Empty;
    public string SubHeader { get; set; } = string.Empty;
    public string ThumbnailURL { get; set; } = string.Empty;
    public LegacyRegionTypeEnum GroupRegion { get; set; }
    public LegacySatelliteItem[] SatelliteItems { get; set; } = Array.Empty<LegacySatelliteItem>();
}

[XmlType(TypeName = "OVERLAYS", Namespace = LegacySoapMapper.ServiceNamespace)]
public class LegacyOverlay
{
    public string Description { get; set; } = string.Empty;
    public string URL { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string TextColor { get; set; } = string.Empty;
    public string HideLayers { get; set; } = string.Empty;
    public double LatitudeStart { get; set; }
    public double LongitudeStart { get; set; }
    public double LatitudeEnd { get; set; }
    public double LongitudeEnd { get; set; }
    public string AnimationWildCard { get; set; } = string.Empty;
    public int MaxFrames { get; set; }
    public int MaxLoops { get; set; }
}

[XmlType(TypeName = "ImageLink", Namespace = LegacySoapMapper.ServiceNamespace)]
public class LegacyImageLink
{
    public int ImageLinkID { get; set; }
    public int ImageLinkType { get; set; }
    public int StormID { get; set; }
    public string URL { get; set; } = string.Empty;
    public DateTime DateUpated { get; set; }
}

[XmlType(TypeName = "GISFile", Namespace = LegacySoapMapper.ServiceNamespace)]
public class LegacyGISFile
{
    public string URL { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public LegacyGISFileTypes GISFileType { get; set; }
}

[XmlType(TypeName = "GISFileTypes", Namespace = LegacySoapMapper.ServiceNamespace)]
public enum LegacyGISFileTypes
{
    Unknown,
}

[XmlType(TypeName = "ReturnMessage", Namespace = LegacySoapMapper.ServiceNamespace)]
public class LegacyReturnMessage
{
    public int MessageNumber { get; set; }
    public string Message { get; set; } = string.Empty;
}

[XmlType(TypeName = "Gadget", Namespace = LegacySoapMapper.ServiceNamespace)]
public class LegacyGadget : LegacyBaseBusiness
{
    public double Timer { get; set; }
    public LegacyVersionInfo VersionInfo { get; set; } = new();
    public LegacyStorm[] Storms { get; set; } = Array.Empty<LegacyStorm>();
}

[XmlType(TypeName = "VersionInfo", Namespace = LegacySoapMapper.ServiceNamespace)]
public class LegacyVersionInfo : LegacyBaseBusiness
{
    public int SharewareLimit { get; set; }
    public bool RunningLatestVersion { get; set; }
    public bool RequiredUpdate { get; set; }
    public string LatestVersion { get; set; } = string.Empty;
    public string DownloadLocation { get; set; } = string.Empty;
    public LegacyReturnMessage ReturnMessage { get; set; } = new();
}

[XmlType(TypeName = "Storm", Namespace = LegacySoapMapper.ServiceNamespace)]
public class LegacyStorm : LegacyBaseBusiness
{
    public LegacyGISFile[] GISFiles { get; set; } = Array.Empty<LegacyGISFile>();
    public LegacyImageLink[] ImageLinks { get; set; } = Array.Empty<LegacyImageLink>();
    public LegacyCoordinate[] Coordinates { get; set; } = Array.Empty<LegacyCoordinate>();
    public int ID { get; set; }
    public int StormID { get; set; }
    public string Name { get; set; } = string.Empty;
    public string NameYear { get; set; } = string.Empty;
    public string Region { get; set; } = string.Empty;
    public int Year { get; set; }
    public bool Active { get; set; }
    public LegacyStormTypeEnum StormType { get; set; }
    public bool IsModified { get; set; }
    public bool EmailAlertsSent { get; set; }
    public int StormNumber { get; set; }
    public string Details { get; set; } = string.Empty;
    public string ImageURL { get; set; } = string.Empty;
}

[XmlType(TypeName = "Coordinate", Namespace = LegacySoapMapper.ServiceNamespace)]
public class LegacyCoordinate : LegacyBaseBusiness
{
    public int StormID { get; set; }
    public int StormNumber { get; set; }
    public string AdvisoryNumber { get; set; } = string.Empty;
    public float Latitude { get; set; }
    public float Longitude { get; set; }
    public int WindSpeed { get; set; }
    public int WindSpeedKts { get; set; }
    public int SpeedTravel { get; set; }
    public int SpeedTravelKts { get; set; }
    public int Pressure { get; set; }
    public int Direction { get; set; }
    public int DirectionDegrees { get; set; }
    public string DirectionStr { get; set; } = string.Empty;
    public string Heading { get; set; } = string.Empty;
    public LegacyStormTypeEnum StormType { get; set; }
    public string UTCOffset { get; set; } = string.Empty;
    public DateTime DateTime { get; set; }
}

 [XmlType(TypeName = "StormTypeEnum", Namespace = LegacySoapMapper.ServiceNamespace)]
public enum LegacyStormTypeEnum
{
    Unknown,
    TropicalDepression,
    SubTropicalStorm,
    TropicalStorm,
    Hurricane,
}

[XmlType(TypeName = "User", Namespace = LegacySoapMapper.ServiceNamespace)]
public class LegacyUser : LegacyBaseBusiness
{
    public LegacyVersionInfo VersionInfo { get; set; } = new();
    public LegacyAppLinksType LoginMessageType { get; set; }
    public bool ShowLoginMessage { get; set; }
    public bool LoggedIn { get; set; }
    public bool NeedToRegister { get; set; }
    public bool RunningLatestVersion { get; set; }
    public LegacyAppLink[] AppLinks { get; set; } = Array.Empty<LegacyAppLink>();
    public LegacyReturnMessage ReturnMessage { get; set; } = new();
}

[XmlType(TypeName = "AppLink", Namespace = LegacySoapMapper.ServiceNamespace)]
public class LegacyAppLink
{
    public int ID { get; set; }
    public string URL { get; set; } = string.Empty;
    public LegacyRegionType Region { get; set; }
    public LegacyAppLinksType appLinksType { get; set; }
}

 [XmlType(TypeName = "RegionType", Namespace = LegacySoapMapper.ServiceNamespace)]
public enum LegacyRegionType
{
    All,
    Atlantic,
    EasterPacific,
}

 [XmlType(TypeName = "AppLinksType", Namespace = LegacySoapMapper.ServiceNamespace)]
public enum LegacyAppLinksType
{
    All = 0,
    SatelliteImages = 1,
    TropicalWeatherOutlook = 2,
    TropicalWeatherDiscussion = 3,
    Summary = 4,
    ForecastAdvisory = 5,
    StormDiscussion = 6,
    PublicAdvisory = 7,
    NOAARecon = 8,
    EmailPhoneAlerts = 9,
    WirelessTracking = 10,
    MyAccountTab = 11,
    OrderPage = 12,
    LoginMessage = 13,
    LoginMessageRegistrationExpired = 14,
    LoginMessageSharewareLimit = 15,
    LoginMessageMultiUser = 16,
    LoginMessageHacker = 17,
    LearnHowToSubscribe = 18,
    HomePage = 19,
    NewsDesktopTab = 20,
    FacebookDesktopTab = 21,
    TwitterDesktopTab = 22,
}

 [XmlType(TypeName = "ApplicationTypeEnum", Namespace = LegacySoapMapper.ServiceNamespace)]
public enum LegacyApplicationTypeEnum
{
    Unknown,
    TrackingTheEye,
    HurricaneSoftwareGadget,
    HurricaneSoftwareIPhone,
    HurricaneSoftwareIPhoneFree,
    HurricaneSoftwareForWindowsPhone,
    HurricaneSoftwareForAndroid,
}

 [XmlType(TypeName = "AlertTypeEnum", Namespace = LegacySoapMapper.ServiceNamespace)]
public enum LegacyAlertTypeEnum
{
    Unknown,
    Email,
    CellPhone,
}

 [XmlType(TypeName = "GadgetType", Namespace = LegacySoapMapper.ServiceNamespace)]
public enum LegacyGadgetType
{
    Unknown,
    Windows,
    iPhone,
}

 [XmlType(TypeName = "ImageLinkType", Namespace = LegacySoapMapper.ServiceNamespace)]
public enum LegacyImageLinkType
{
    All,
    GoesVis,
    GoesIR,
    GoesWV,
    GoesVisFull,
    GoesIRFull,
    GoesWVFull,
    GoesColorVis,
    GoesColorIR,
    GoesColorWV,
    SSTDaily,
    SST7Day,
    NationalRadarSmall,
    NationalRadarLarge,
    NavyVis,
    NavyIR,
    NavyWV,
    MeteosatVis,
    MeteosatIR,
    NavyIRAtlanticInvest,
    NHC5DayWatchesAndWarnings,
    NHCHurricaneForceWinds,
    NHC50KnotWind,
    NHCTropicalStormForceWind,
    NHCWindSpeedTable,
    NHC3DayWatchesAndWarnings,
    NHCMarinersRule,
    NHCWindHistory,
}