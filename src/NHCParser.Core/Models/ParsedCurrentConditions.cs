namespace NHCParser.Core.Models;

public sealed class ParsedCurrentConditions
{
    public double Latitude { get; init; }

    public double Longitude { get; init; }

    public int? PressureMb { get; init; }

    public int? WindSpeedKts { get; init; }

    public int? WindSpeedMph { get; init; }

    public int? MovementDirectionDegrees { get; init; }

    public int? MovementDirectionCode { get; init; }

    public string? MovementHeading { get; init; }

    public int? MovementSpeedKts { get; init; }

    public int? MovementSpeedMph { get; init; }

    public ParsedStormType StormType { get; init; }
}