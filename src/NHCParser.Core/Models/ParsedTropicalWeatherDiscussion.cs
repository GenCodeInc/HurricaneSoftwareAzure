namespace NHCParser.Core.Models;

public sealed class ParsedTropicalWeatherDiscussion
{
    public IReadOnlyList<ParsedPointOfInterest> PointsOfInterest { get; init; } = Array.Empty<ParsedPointOfInterest>();
}