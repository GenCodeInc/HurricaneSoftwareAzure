using System.Data;
using System.ServiceModel;
using System.Xml.Serialization;

namespace TropicalStorms.Api.LegacyCompat;

[ServiceContract(Namespace = LegacySoapMapper.ServiceNamespace, Name = "TropicalStormInfoSoap")]
[XmlSerializerFormat]
public interface ILegacyTropicalStormsSoapService
{
    [OperationContract(Action = LegacySoapMapper.ServiceNamespace + "/HelloWorld", ReplyAction = "*")]
    string HelloWorld();

    [OperationContract(Action = LegacySoapMapper.ServiceNamespace + "/GetGISData", ReplyAction = "*")]
    LegacyGisData[] GetGISData();

    [OperationContract(Action = LegacySoapMapper.ServiceNamespace + "/GetStormOverlays", ReplyAction = "*")]
    LegacyOverlay[] GetStormOverlays();

    [OperationContract(Action = LegacySoapMapper.ServiceNamespace + "/MobileSatelliteTabAndroid", ReplyAction = "*")]
    LegacySatelliteGroup[] MobileSatelliteTabAndroid(int appTypeID, int regionType);

    [OperationContract(Action = LegacySoapMapper.ServiceNamespace + "/MobileTabs", ReplyAction = "*")]
    LegacyMobileTabGroup[] MobileTabs(int appTypeID, int regionType, int tabToShowOn);

    [OperationContract(Action = LegacySoapMapper.ServiceNamespace + "/MobileSatelliteTab", ReplyAction = "*")]
    LegacySatelliteGroup[] MobileSatelliteTab(LegacyApplicationTypeEnum appTypeID, LegacyRegionTypeEnum regionType);

    [OperationContract(Action = LegacySoapMapper.ServiceNamespace + "/CreateAlerts", ReplyAction = "*")]
    string CreateAlerts(int alertTypeID, string email, int appTypeID, string deviceID, int regionType);

    [OperationContract(Action = LegacySoapMapper.ServiceNamespace + "/CreateAlert", ReplyAction = "*")]
    string CreateAlert(LegacyAlertTypeEnum alertTypeID, string email, LegacyApplicationTypeEnum appTypeID, string deviceID, LegacyRegionTypeEnum regionType);

    [OperationContract(Action = LegacySoapMapper.ServiceNamespace + "/RemoveAlert", ReplyAction = "*")]
    string RemoveAlert(string value, LegacyRegionTypeEnum region);

    [OperationContract(Action = LegacySoapMapper.ServiceNamespace + "/PointsOfInterest", ReplyAction = "*")]
    LegacyPointOfInterest[] PointsOfInterest();

    [OperationContract(Action = LegacySoapMapper.ServiceNamespace + "/GetRegCode", ReplyAction = "*")]
    string GetRegCode(string userID);

    [OperationContract(Action = LegacySoapMapper.ServiceNamespace + "/RetrieveRegistration", ReplyAction = "*")]
    LegacyReturnMessage RetrieveRegistration(string Email);

    [OperationContract(Action = LegacySoapMapper.ServiceNamespace + "/LoginUser", ReplyAction = "*")]
    LegacyUser LoginUser(string userID, string registrationNumber, string osBinaryTime, int numberOfTimesLoggedIn, bool isRegistered, string TTEVersion, string promo);

    [OperationContract(Action = LegacySoapMapper.ServiceNamespace + "/AppLinksAndroid", ReplyAction = "*")]
    LegacyAppLink[] AppLinksAndroid(string userID, string registrationNumber, string osBinaryTime, int numberOfTimesLoggedIn, string promo, int appLinkType, int regionType);

    [OperationContract(Action = LegacySoapMapper.ServiceNamespace + "/AppLinks", ReplyAction = "*")]
    LegacyAppLink[] AppLinks(string userID, string registrationNumber, string osBinaryTime, int numberOfTimesLoggedIn, string promo, LegacyAppLinksType appLinkType, LegacyRegionTypeEnum regionType);

    [OperationContract(Action = LegacySoapMapper.ServiceNamespace + "/VersionCheck", ReplyAction = "*")]
    LegacyVersionInfo VersionCheck(int ApplicationType, string version, string promo, bool getzip);

    [OperationContract(Action = LegacySoapMapper.ServiceNamespace + "/ValidateUser", ReplyAction = "*")]
    LegacyReturnMessage ValidateUser(string userID, string registrationNumber);

    [OperationContract(Action = LegacySoapMapper.ServiceNamespace + "/GetTimeStamp", ReplyAction = "*")]
    DateTime GetTimeStamp(string region);

    [OperationContract(Action = LegacySoapMapper.ServiceNamespace + "/StormNames", ReplyAction = "*")]
    LegacyStorm[] StormNames(string username, string password, string region, bool activeOnly);

    [OperationContract(Action = LegacySoapMapper.ServiceNamespace + "/GetDistanceBetweenPoints", ReplyAction = "*")]
    double GetDistanceBetweenPoints(double startLat, double startLong, double endLat, double endLon);

    [OperationContract(Action = LegacySoapMapper.ServiceNamespace + "/GetStorm", ReplyAction = "*")]
    LegacyStorm GetStorm(string username, string password, int stormID, bool withImageLinks);

    [OperationContract(Action = LegacySoapMapper.ServiceNamespace + "/GetGadget", ReplyAction = "*")]
    LegacyGadget GetGadget(string region, LegacyGadgetType GadgetType, string Version);

    [OperationContract(Action = LegacySoapMapper.ServiceNamespace + "/Storms", ReplyAction = "*")]
    LegacyStorm[] Storms(string username, string password, string StormsToDownload, string region, bool withImageLinks, bool activeOnly, bool lastCoordinateOnly, bool omitForecastsToo);

    [OperationContract(Action = LegacySoapMapper.ServiceNamespace + "/GetCoordinates", ReplyAction = "*")]
    LegacyCoordinate[] GetCoordinates(string username, string password, int StormID);

    [OperationContract(Action = LegacySoapMapper.ServiceNamespace + "/ImageLinks", ReplyAction = "*")]
    LegacyImageLink[] ImageLinks(string username, string password, int stormID, LegacyImageLinkType imageLinkType);

    [OperationContract(Action = LegacySoapMapper.ServiceNamespace + "/GetStormNames", ReplyAction = "*")]
    LegacyStormName[] GetStormNames(string username, string password, string region);

    [OperationContract(Action = LegacySoapMapper.ServiceNamespace + "/GetStormsDataset", ReplyAction = "*")]
    DataSet GetStormsDataset(string username, string password, string StormsToDownload, string region);
}