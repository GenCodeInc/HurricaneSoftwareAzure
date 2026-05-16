using System.Globalization;
using System.Text.RegularExpressions;
using NHCParser.Core.Models;

namespace NHCParser.Core.Services;

internal sealed partial class IntermediateAdvisoryParser
{
    public ParsedAdvisory Parse(string content, IReadOnlyCollection<string>? validNames = null)
    {
        var collapsed = NhcParserText.CollapseWhitespace(content);
        var locationMatch = LocationRegex().Match(collapsed);
        var windMatch = WindRegex().Match(collapsed);
        var movementMatch = MovementRegex().Match(collapsed);
        var pressureMatch = PressureRegex().Match(collapsed);

        if (!locationMatch.Success || !windMatch.Success || !movementMatch.Success || !pressureMatch.Success)
        {
            throw new InvalidOperationException("Could not parse intermediate advisory summary section.");
        }

        var windMph = int.Parse(windMatch.Groups["windMph"].Value, CultureInfo.InvariantCulture);
        int? movementDegrees = null;
        if (movementMatch.Groups["degrees"].Success)
        {
            movementDegrees = int.Parse(movementMatch.Groups["degrees"].Value, CultureInfo.InvariantCulture);
        }

        var heading = movementMatch.Groups["heading"].Value.Trim().ToUpperInvariant();
        var current = new ParsedCurrentConditions
        {
            Latitude = NhcParserText.ParseCoordinate(locationMatch.Groups["latitude"].Value, locationMatch.Groups["latitudeHemisphere"].Value),
            Longitude = NhcParserText.ParseCoordinate(locationMatch.Groups["longitude"].Value, locationMatch.Groups["longitudeHemisphere"].Value),
            PressureMb = int.Parse(pressureMatch.Groups["pressureMb"].Value, CultureInfo.InvariantCulture),
            WindSpeedMph = windMph,
            WindSpeedKts = NhcParserText.MphToKts(windMph),
            MovementHeading = heading,
            MovementDirectionDegrees = movementDegrees,
            MovementDirectionCode = NhcParserText.HeadingToDirectionCode(heading),
            MovementSpeedMph = movementMatch.Groups["speedMph"].Success ? int.Parse(movementMatch.Groups["speedMph"].Value, CultureInfo.InvariantCulture) : 0,
            MovementSpeedKts = movementMatch.Groups["speedMph"].Success ? NhcParserText.MphToKts(int.Parse(movementMatch.Groups["speedMph"].Value, CultureInfo.InvariantCulture)) : 0,
            StormType = NhcParserText.DeriveStormType(windMph),
        };

        return new ParsedAdvisory
        {
            Kind = NhcAdvisoryKind.Intermediate,
            Region = NhcParserText.DetectRegion(content),
            StormName = NhcParserText.ParseStormName(content, validNames),
            StormNumber = NhcParserText.ParseStormNumber(content),
            AdvisoryNumber = NhcParserText.ParseAdvisoryNumber(content),
            IssuedAtUtc = NhcParserText.ParseIssuedAtUtc(content),
            Current = current,
            IsFinalAdvisory = NhcParserText.IsFinalAdvisory(content),
        };
    }

    [GeneratedRegex(@"LOCATION\.\.\.(?<latitude>\d+(?:\.\d+)?)(?<latitudeHemisphere>[NS])\s+(?<longitude>\d+(?:\.\d+)?)(?<longitudeHemisphere>[EW])", RegexOptions.IgnoreCase | RegexOptions.Compiled)]
    private static partial Regex LocationRegex();

    [GeneratedRegex(@"MAXIMUM SUSTAINED WINDS\.\.\.(?<windMph>\d+)\s+MPH", RegexOptions.IgnoreCase | RegexOptions.Compiled)]
    private static partial Regex WindRegex();

    [GeneratedRegex(@"PRESENT MOVEMENT\.\.\.(?<heading>[A-Z-]+)(?:\s+OR\s+(?<degrees>\d+)\s+DEGREES)?(?:\s+AT\s+(?<speedMph>\d+)\s+MPH)?", RegexOptions.IgnoreCase | RegexOptions.Compiled)]
    private static partial Regex MovementRegex();

    [GeneratedRegex(@"MINIMUM CENTRAL PRESSURE\.\.\.(?<pressureMb>\d+)\s+MB", RegexOptions.IgnoreCase | RegexOptions.Compiled)]
    private static partial Regex PressureRegex();
}