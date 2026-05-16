namespace TTENET.TTEBusiness.Core.Models;

public sealed class VersionInfoResult
{
    public int SharewareLimit { get; set; }

    public bool RunningLatestVersion { get; set; }

    public bool RequiredUpdate { get; set; }

    public string LatestVersion { get; set; } = string.Empty;

    public string DownloadLocation { get; set; } = string.Empty;

    public ReturnMessage ReturnMessage { get; set; } = new();
}