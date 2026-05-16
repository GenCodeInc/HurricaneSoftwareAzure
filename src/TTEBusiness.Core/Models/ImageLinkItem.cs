namespace TTENET.TTEBusiness.Core.Models;

public sealed class ImageLinkItem
{
    public int ImageLinkId { get; set; }

    public int ImageLinkType { get; set; }

    public int StormId { get; set; }

    public string Url { get; set; } = string.Empty;

    public DateTime DateUpdated { get; set; }
}