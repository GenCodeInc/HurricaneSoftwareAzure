namespace TTENET.TTEBusiness.Core.Models;

public sealed class PersistAdvisoryRequest
{
    public required string StormName { get; init; }

    public required string StormIdentifier { get; init; }

    public required int StormNumber { get; init; }

    public required string AdvisoryNumber { get; init; }

    public required int Year { get; init; }

    public required int RegionType { get; init; }

    public required int StormType { get; init; }

    public required bool IsFinalAdvisory { get; init; }

    public required DateTimeOffset IssuedAtUtc { get; init; }

    public required double Latitude { get; init; }

    public required double Longitude { get; init; }

    public required int WindSpeed { get; init; }

    public required int Pressure { get; init; }

    public required int SpeedOfTravel { get; init; }

    public required int Direction { get; init; }

    public required bool UpdateAdvisorySlot { get; init; }
}
