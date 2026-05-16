namespace TTENET.TTEBusiness.Core.Models;

public sealed class PointOfInterestItem
{
    public int Id { get; set; }

    public float Latitude { get; set; }

    public float Longitude { get; set; }

    public string RelatedText { get; set; } = string.Empty;

    public int Type { get; set; }
}