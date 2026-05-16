namespace TTENET.TTEBusiness.Core.Models;

public sealed class GisDataItem
{
    public string Title { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public string Url { get; set; } = string.Empty;

    public bool Active { get; set; }
}