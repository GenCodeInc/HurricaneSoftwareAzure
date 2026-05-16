namespace TTENET.TTEBusiness.Core.Models;

public sealed class GadgetResult
{
    public double Timer { get; set; }

    public VersionInfoResult VersionInfo { get; set; } = new();

    public IReadOnlyList<StormDetailItem> Storms { get; set; } = Array.Empty<StormDetailItem>();
}