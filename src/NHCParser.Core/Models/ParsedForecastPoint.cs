namespace NHCParser.Core.Models;

public sealed class ParsedForecastPoint
{
    public DateTimeOffset ValidAtUtc { get; init; }

    public bool IsDissipated { get; init; }

    public double? Latitude { get; init; }

    public double? Longitude { get; init; }

    public int? WindSpeedKts { get; init; }

    public int? WindSpeedMph { get; init; }
}