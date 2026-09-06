using System.Globalization;
using System.Text.RegularExpressions;
using NHCParser.Core.Models;

namespace NHCParser.Core.Services;

internal sealed partial class NormalAdvisoryParser
{
    public ParsedAdvisory Parse(string content, IReadOnlyCollection<string>? validNames = null)
    {
        var issuedAtUtc = NhcParserText.ParseIssuedAtUtc(content);
        var currentLocation = CurrentLocationRegex().Match(NhcParserText.CollapseWhitespace(content));
        if (!currentLocation.Success)
        {
            throw new InvalidOperationException("Could not parse current advisory position for normal advisory.");
        }

        var pressureMatch = PressureRegex().Match(NhcParserText.CollapseWhitespace(content));
        var windMatch = WindRegex().Match(NhcParserText.CollapseWhitespace(content));
        var movementMatch = MovementRegex().Match(NhcParserText.CollapseWhitespace(content));

        if (!pressureMatch.Success || !windMatch.Success)
        {
            throw new InvalidOperationException("Could not parse current wind or pressure for normal advisory.");
        }

        int? movementDegrees = null;
        int? movementDirectionCode = null;
        int? movementSpeedKts = null;
        int? movementSpeedMph = null;
        string? movementHeading = null;

        if (movementMatch.Success)
        {
            movementHeading = movementMatch.Groups["heading"].Value.Trim().ToUpperInvariant();
            if (movementHeading.StartsWith("THE ", StringComparison.Ordinal))
            {
                movementHeading = movementHeading[4..];
            }

            movementDegrees = int.Parse(movementMatch.Groups["degrees"].Value, CultureInfo.InvariantCulture);
            movementDirectionCode = NhcParserText.DegreesToDirectionCode(movementDegrees.Value);
            movementSpeedKts = int.Parse(movementMatch.Groups["speedKts"].Value, CultureInfo.InvariantCulture);
            movementSpeedMph = NhcParserText.KtsToMph(movementSpeedKts.Value);
        }
        else if (content.Contains("PRESENT MOVEMENT IS STATIONARY", StringComparison.OrdinalIgnoreCase))
        {
            movementHeading = "STATIONARY";
            movementDegrees = 0;
            movementDirectionCode = 0;
            movementSpeedKts = 0;
            movementSpeedMph = 0;
        }

        var windSpeedKts = int.Parse(windMatch.Groups["windKts"].Value, CultureInfo.InvariantCulture);
        var current = new ParsedCurrentConditions
        {
            Latitude = NhcParserText.ParseCoordinate(currentLocation.Groups["latitude"].Value, currentLocation.Groups["latitudeHemisphere"].Value),
            Longitude = NhcParserText.ParseCoordinate(currentLocation.Groups["longitude"].Value, currentLocation.Groups["longitudeHemisphere"].Value),
            PressureMb = int.Parse(pressureMatch.Groups["pressureMb"].Value, CultureInfo.InvariantCulture),
            WindSpeedKts = windSpeedKts,
            WindSpeedMph = NhcParserText.KtsToMph(windSpeedKts),
            MovementDirectionDegrees = movementDegrees,
            MovementDirectionCode = movementDirectionCode,
            MovementHeading = movementHeading,
            MovementSpeedKts = movementSpeedKts,
            MovementSpeedMph = movementSpeedMph,
            StormType = NhcParserText.DeriveStormType(NhcParserText.KtsToMph(windSpeedKts)),
        };

        return new ParsedAdvisory
        {
            Kind = NhcAdvisoryKind.Normal,
            Region = NhcParserText.DetectRegion(content),
            StormName = NhcParserText.ParseStormName(content, validNames),
            StormIdentifier = NhcParserText.ParseStormIdentifier(content),
            StormNumber = NhcParserText.ParseStormNumber(content),
            StormYear = NhcParserText.ParseStormYear(content),
            AdvisoryNumber = NhcParserText.ParseAdvisoryNumber(content),
            IssuedAtUtc = issuedAtUtc,
            Current = current,
            IsFinalAdvisory = NhcParserText.IsFinalAdvisory(content),
            ForecastPoints = ParseForecastPoints(content, issuedAtUtc),
        };
    }

    private static IReadOnlyList<ParsedForecastPoint> ParseForecastPoints(string content, DateTimeOffset issuedAtUtc)
    {
        var forecastPoints = new List<ParsedForecastPoint>();
        var normalizedContent = NhcParserText.CollapseWhitespace(content);
        foreach (Match blockMatch in ForecastEntryRegex().Matches(normalizedContent))
        {
            var validAtUtc = NhcParserText.BuildForecastDate(
                issuedAtUtc,
                int.Parse(blockMatch.Groups["day"].Value, CultureInfo.InvariantCulture),
                blockMatch.Groups["time"].Value);

            var state = NhcParserText.CollapseWhitespace(blockMatch.Groups["state"].Value);
            if (state.StartsWith("DISSIPATED", StringComparison.OrdinalIgnoreCase))
            {
                forecastPoints.Add(new ParsedForecastPoint
                {
                    ValidAtUtc = validAtUtc,
                    IsDissipated = true,
                });

                continue;
            }

            var locationMatch = ForecastLocationRegex().Match(state);
            if (!locationMatch.Success)
            {
                continue;
            }

            var windMatch = ForecastWindRegex().Match(state);
            int? windKts = null;
            if (windMatch.Success)
            {
                windKts = int.Parse(windMatch.Groups["windKts"].Value, CultureInfo.InvariantCulture);
            }

            forecastPoints.Add(new ParsedForecastPoint
            {
                ValidAtUtc = validAtUtc,
                Latitude = NhcParserText.ParseCoordinate(locationMatch.Groups["latitude"].Value, locationMatch.Groups["latitudeHemisphere"].Value),
                Longitude = NhcParserText.ParseCoordinate(locationMatch.Groups["longitude"].Value, locationMatch.Groups["longitudeHemisphere"].Value),
                WindSpeedKts = windKts,
                WindSpeedMph = windKts.HasValue ? NhcParserText.KtsToMph(windKts.Value) : null,
            });
        }

        return forecastPoints;
    }

    [GeneratedRegex(@"CENTER LOCATED NEAR\s+(?<latitude>\d+(?:\.\d+)?)(?<latitudeHemisphere>[NS])\s+(?<longitude>\d+(?:\.\d+)?)(?<longitudeHemisphere>[EW])", RegexOptions.IgnoreCase | RegexOptions.Compiled)]
    private static partial Regex CurrentLocationRegex();

    [GeneratedRegex(@"ESTIMATED MINIMUM CENTRAL PRESSURE\s+(?<pressureMb>\d+)\s+MB", RegexOptions.IgnoreCase | RegexOptions.Compiled)]
    private static partial Regex PressureRegex();

    [GeneratedRegex(@"MAX SUSTAINED WINDS\s+(?<windKts>\d+)\s+KT", RegexOptions.IgnoreCase | RegexOptions.Compiled)]
    private static partial Regex WindRegex();

    [GeneratedRegex(@"PRESENT MOVEMENT TOWARD\s+(?<heading>[A-Z\s-]+?)\s+OR\s+(?<degrees>\d+)\s+DEGREES\s+AT\s+(?<speedKts>\d+)\s+KT", RegexOptions.IgnoreCase | RegexOptions.Compiled)]
    private static partial Regex MovementRegex();

    [GeneratedRegex(@"(?ims)(?:FORECAST|OUTLOOK)\s+VALID\s+(?<day>\d{2})/(?<time>\d{4})Z(?:\.\.\.)?\s*(?<state>.*?)(?=(?:FORECAST|OUTLOOK)\s+VALID|REQUEST FOR|\$\$|FORECASTER|THIS IS THE LAST|\z)", RegexOptions.IgnoreCase | RegexOptions.Compiled)]
    private static partial Regex ForecastEntryRegex();

    [GeneratedRegex(@"(?<latitude>\d+(?:\.\d+)?)(?<latitudeHemisphere>[NS])\s+(?<longitude>\d+(?:\.\d+)?)(?<longitudeHemisphere>[EW])", RegexOptions.IgnoreCase | RegexOptions.Compiled)]
    private static partial Regex ForecastLocationRegex();

    [GeneratedRegex(@"MAX WIND\s+(?<windKts>\d+)\s+KT", RegexOptions.IgnoreCase | RegexOptions.Compiled)]
    private static partial Regex ForecastWindRegex();
}
