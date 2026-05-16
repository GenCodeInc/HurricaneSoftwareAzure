namespace TTENET.TTEBusiness.Core.Models;

public sealed class UserResult
{
    public VersionInfoResult VersionInfo { get; set; } = new();

    public int LoginMessageType { get; set; }

    public bool ShowLoginMessage { get; set; }

    public bool LoggedIn { get; set; }

    public int SharewareLimit { get; set; }

    public bool NeedToRegister { get; set; }

    public bool RunningLatestVersion { get; set; }

    public IReadOnlyList<AppLinkItem> AppLinks { get; set; } = Array.Empty<AppLinkItem>();

    public ReturnMessage ReturnMessage { get; set; } = new();
}