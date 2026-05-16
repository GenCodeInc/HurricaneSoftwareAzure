namespace NHCParser.Core.Models;

public sealed class ParsedPointOfInterest
{
    public required double Latitude { get; init; }

    public required double Longitude { get; init; }

    public required ParsedPointOfInterestType Type { get; init; }

    public required int SequenceAdded { get; init; }

    public required string RelatedText { get; init; }
}