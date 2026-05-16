namespace TTENET.TTEBusiness.Core.Models;

public sealed class SatelliteGroupItem
{
    public int ID { get; set; }

    public string Header { get; set; } = string.Empty;

    public string SubHeader { get; set; } = string.Empty;

    public string ThumbnailURL { get; set; } = string.Empty;

    public int GroupRegion { get; set; }

    public IReadOnlyList<SatelliteItem> SatelliteItems { get; set; } = Array.Empty<SatelliteItem>();
}