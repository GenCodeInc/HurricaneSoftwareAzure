namespace TTENET.TTEBusiness.Core.Models;

public sealed class SatelliteItem
{
    public int ID { get; set; }

    public int SatelliteGroupID { get; set; }

    public string ThumbnailURL { get; set; } = string.Empty;

    public string URL { get; set; } = string.Empty;

    public string Text { get; set; } = string.Empty;
}