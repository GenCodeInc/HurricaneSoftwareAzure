namespace TropicalStorms.Api.Models;

public sealed class OverlayDto
{
    public string Description { get; set; } = string.Empty;

    public string Url { get; set; } = string.Empty;

    public string Type { get; set; } = string.Empty;

    public string TextColor { get; set; } = string.Empty;

    public string HideLayers { get; set; } = string.Empty;

    public double LatitudeStart { get; set; }

    public double LongitudeStart { get; set; }

    public double LatitudeEnd { get; set; }

    public double LongitudeEnd { get; set; }

    public string AnimationWildCard { get; set; } = string.Empty;

    public int MaxFrames { get; set; }

    public int MaxLoops { get; set; }
}