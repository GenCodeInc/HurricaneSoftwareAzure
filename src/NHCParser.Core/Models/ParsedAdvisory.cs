namespace NHCParser.Core.Models;

public sealed class ParsedAdvisory
{
    public required NhcAdvisoryKind Kind { get; init; }

    public required NhcRegion Region { get; init; }

    public required string StormName { get; init; }

    public required int StormNumber { get; init; }

    public required string AdvisoryNumber { get; init; }

    public required DateTimeOffset IssuedAtUtc { get; init; }

    public required ParsedCurrentConditions Current { get; init; }

    public bool IsFinalAdvisory { get; init; }

    public IReadOnlyList<ParsedForecastPoint> ForecastPoints { get; init; } = Array.Empty<ParsedForecastPoint>();
}