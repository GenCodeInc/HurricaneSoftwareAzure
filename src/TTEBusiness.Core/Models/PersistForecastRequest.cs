namespace TTENET.TTEBusiness.Core.Models;

public sealed class PersistForecastRequest
{
    public required string StormName { get; init; }

    public required int StormNumber { get; init; }

    public required int Year { get; init; }

    public required int RegionType { get; init; }

    public required IReadOnlyList<PersistForecastPointRequest> ForecastPoints { get; init; }
}

public sealed class PersistForecastPointRequest
{
    public required string AdvisoryNumber { get; init; }

    public required DateTimeOffset ValidAtUtc { get; init; }

    public required double Latitude { get; init; }

    public required double Longitude { get; init; }

    public required int WindSpeed { get; init; }
}