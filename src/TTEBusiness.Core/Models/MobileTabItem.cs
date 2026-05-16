namespace TTENET.TTEBusiness.Core.Models;

public sealed class MobileTabItem
{
    public int ID { get; set; }

    public int MobileTabGroupID { get; set; }

    public string ThumbnailURL { get; set; } = string.Empty;

    public string URL { get; set; } = string.Empty;

    public string Text { get; set; } = string.Empty;

    public int ItemType { get; set; }
}