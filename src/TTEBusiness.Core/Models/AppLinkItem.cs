namespace TTENET.TTEBusiness.Core.Models;

public sealed class AppLinkItem
{
    public int Id { get; set; }

    public string Url { get; set; } = string.Empty;

    public int RegionType { get; set; }

    public int AppLinkType { get; set; }

    public bool Active { get; set; }
}