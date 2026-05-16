using System.Globalization;
using System.Text.RegularExpressions;
using NHCParser.Core.Models;

namespace NHCParser.Core.Services;

public sealed partial class NhcTropicalWeatherDiscussionParser : INhcTropicalWeatherDiscussionParser
{
    public ParsedTropicalWeatherDiscussion Parse(string content)
    {
        var sequence = content.Contains("AXNT20", StringComparison.OrdinalIgnoreCase)
            ? 100
            : content.Contains("AXPZ20", StringComparison.OrdinalIgnoreCase)
                ? 200
                : 0;

        var points = new List<ParsedPointOfInterest>();
        ParseWaves(content, points, ref sequence);
        ParseRegionalPoints(content, points, ref sequence);

        return new ParsedTropicalWeatherDiscussion
        {
            PointsOfInterest = points.OrderBy(point => point.SequenceAdded).ToArray(),
        };
    }

    private static void ParseWaves(string content, List<ParsedPointOfInterest> points, ref int sequence)
    {
        var remaining = content;
        for (var index = 0; index < 100; index++)
        {
            var waveIndex = IndexOfAny(remaining, "TROPICAL WAVE ", "LONG WAVE ");
            if (waveIndex < 0)
            {
                break;
            }

            var dataWork = remaining[waveIndex..];
            var waveEndIndex = FindWaveEnd(dataWork);
            var waveString = waveEndIndex > 0 ? dataWork[..waveEndIndex] : dataWork;
            var normalized = waveString.Replace("\r", " ").Replace("\n", " ").Replace("...", " ");
            var words = normalized.Split(' ', StringSplitOptions.RemoveEmptyEntries);

            string[]? longitudes = null;
            double latitude = 0;
            var foundKey = false;

            foreach (var rawWord in words)
            {
                var word = rawWord.Trim().Trim(',', '.', ';', ':');
                if (foundKey && word.EndsWith('W'))
                {
                    var longitudeToken = word.Replace("W", string.Empty, StringComparison.OrdinalIgnoreCase);
                    if (longitudeToken.Contains('/'))
                    {
                        longitudes = longitudeToken.Split('/', StringSplitOptions.RemoveEmptyEntries);
                    }
                    else if (longitudeToken.Contains('-'))
                    {
                        longitudes = longitudeToken.Split('-', StringSplitOptions.RemoveEmptyEntries);
                    }
                    else
                    {
                        longitudes = [longitudeToken];
                    }
                }

                if (foundKey && word.EndsWith('N'))
                {
                    _ = double.TryParse(word[..^1], CultureInfo.InvariantCulture, out latitude);
                }

                if (latitude != 0 && longitudes is not null)
                {
                    break;
                }

                if (word.Equals("ALONG", StringComparison.OrdinalIgnoreCase) ||
                    word.Equals("ESTIMATED", StringComparison.OrdinalIgnoreCase) ||
                    word.Equals("OF", StringComparison.OrdinalIgnoreCase))
                {
                    foundKey = true;
                }
            }

            if (latitude != 0 && longitudes is not null)
            {
                foreach (var longitude in longitudes)
                {
                    if (!double.TryParse(longitude, CultureInfo.InvariantCulture, out var parsedLongitude))
                    {
                        continue;
                    }

                    sequence++;
                    points.Add(new ParsedPointOfInterest
                    {
                        Latitude = latitude,
                        Longitude = -parsedLongitude,
                        Type = ParsedPointOfInterestType.TropicalWave,
                        SequenceAdded = sequence,
                        RelatedText = normalized.Trim(),
                    });
                }
            }

            remaining = remaining[(waveIndex + "TROPICAL WAVE ".Length)..];
        }
    }

    private static void ParseRegionalPoints(string content, List<ParsedPointOfInterest> points, ref int sequence)
    {
        foreach (Match sectionMatch in RegionalSectionRegex().Matches(content))
        {
            var normalized = sectionMatch.Value.Replace("\r", " ").Replace("\n", " ").Replace("...", " ");
            foreach (Match match in NearPointRegex().Matches(normalized))
            {
                sequence++;
                points.Add(new ParsedPointOfInterest
                {
                    Latitude = NhcParserText.ParseCoordinate(match.Groups["latitude"].Value, match.Groups["latitudeHemisphere"].Value),
                    Longitude = NhcParserText.ParseCoordinate(match.Groups["longitude"].Value, match.Groups["longitudeHemisphere"].Value),
                    Type = ParsedPointOfInterestType.PointOfInterest,
                    SequenceAdded = sequence,
                    RelatedText = normalized.Trim(),
                });
            }
        }
    }

    private static int FindWaveEnd(string content)
    {
        var candidates = new[]
        {
            content.IndexOf("...ITCZ...", "...ITCZ...".Length, StringComparison.OrdinalIgnoreCase),
            content.IndexOf("TROPICAL WAVE ", "TROPICAL WAVE ".Length, StringComparison.OrdinalIgnoreCase),
            content.IndexOf("LONG WAVE ", "LONG WAVE ".Length, StringComparison.OrdinalIgnoreCase),
        }.Where(index => index > -1).OrderBy(index => index).ToArray();

        return candidates.Length == 0 ? content.Length : candidates[0];
    }

    private static int IndexOfAny(string content, params string[] values)
    {
        return values
            .Select(value => content.IndexOf(value, StringComparison.OrdinalIgnoreCase))
            .Where(index => index > -1)
            .DefaultIfEmpty(-1)
            .Min();
    }

    [GeneratedRegex(@"\bNEAR\s+(?<latitude>\d+(?:\.\d+)?)(?<latitudeHemisphere>[NS])(?<longitude>\d+(?:\.\d+)?)(?<longitudeHemisphere>[EW])\b", RegexOptions.IgnoreCase | RegexOptions.Compiled)]
    private static partial Regex NearPointRegex();

    [GeneratedRegex(@"(?ims)(?:GULF OF MEXICO|CARIBBEAN(?: SEA)?|ATLANTIC(?: OCEAN)?)\s+.*?(?=(?:GULF OF MEXICO|CARIBBEAN(?: SEA)?|ATLANTIC(?: OCEAN)?)|\z)", RegexOptions.IgnoreCase | RegexOptions.Compiled)]
    private static partial Regex RegionalSectionRegex();
}