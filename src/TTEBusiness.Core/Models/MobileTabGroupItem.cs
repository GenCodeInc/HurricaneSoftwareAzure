namespace TTENET.TTEBusiness.Core.Models;

public sealed class MobileTabGroupItem
{
    public int ID { get; set; }

    public string Header { get; set; } = string.Empty;

    public string SubHeader { get; set; } = string.Empty;

    public string ThumbnailURL { get; set; } = string.Empty;

    public int GroupRegion { get; set; }

    public IReadOnlyList<MobileTabItem> MobileTabItems { get; set; } = Array.Empty<MobileTabItem>();

    public int TabToShowOn { get; set; }

    public int ApplicationType { get; set; }
}