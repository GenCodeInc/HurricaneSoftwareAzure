namespace TTENET.TTEBusiness.Core.Models;

public sealed class PersistPointOfInterestRequest
{
    public required double Latitude { get; init; }

    public required double Longitude { get; init; }

    public required int Type { get; init; }

    public required int SequenceAdded { get; init; }

    public required string RelatedText { get; init; }
}