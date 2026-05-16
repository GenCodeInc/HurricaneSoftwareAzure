namespace TTENET.TTEBusiness.Core.Models;

public sealed class StormDetailItem : StormSummaryItem
{
    public IReadOnlyList<CoordinateItem> Coordinates { get; set; } = Array.Empty<CoordinateItem>();

    public IReadOnlyList<ImageLinkItem> ImageLinks { get; set; } = Array.Empty<ImageLinkItem>();

    public string Details { get; set; } = string.Empty;

    public string ImageUrl { get; set; } = string.Empty;

    public bool IsForecast => Name.Contains("FORECAST", StringComparison.OrdinalIgnoreCase);

    public bool IsNamed
    {
        get
        {
            if (string.IsNullOrWhiteSpace(Name) || Name.Length < 2)
            {
                return false;
            }

            return !short.TryParse(Name[..Math.Min(2, Name.Length)], out _);
        }
    }
}