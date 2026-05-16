using System.Globalization;
using System.Text.RegularExpressions;
using NHCParser.Core.Models;

namespace NHCParser.Core.Services;

internal static partial class NhcParserText
{
    private const string AdvisoryNumberSearchText = "ADVISORY NUMBER";

    private static readonly Dictionary<string, int> TimeZoneOffsets = new(StringComparer.OrdinalIgnoreCase)
    {
        ["GMT"] = 0,
        ["UTC"] = 0,
        ["ADT"] = 3,
        ["AST"] = 4,
        ["EDT"] = 4,
        ["EST"] = 5,
        ["CDT"] = 5,
        ["CST"] = 6,
        ["PDT"] = 7,
        ["PST"] = 8,
        ["HDT"] = 9,
        ["HST"] = 10,
    };

    private static readonly HashSet<string> AdvisoryTitleNoiseWords = new(StringComparer.OrdinalIgnoreCase)
    {
        "BULLETIN",
        "POST-TROPICAL",
        "CYCLONE",
        "POTENTIAL",
        "TROPICAL",
        "SUBTROPICAL",
        "STORM",
        "DEPRESSION",
        "HURRICANE",
        "REMNANTS",
        "OF",
        "FORECAST",
        "ADVISORY",
        "NUMBER",
    };

    public static string CollapseWhitespace(string content) => SpaceRegex().Replace(content, " ").Trim();

    public static NhcAdvisoryKind DetectKind(string content)
    {
        if (ContainsAny(content, "TCMAT", "TCMEP", "TCMCP"))
        {
            return NhcAdvisoryKind.Normal;
        }

        if (ContainsAny(content, "TCPAT", "TCPEP", "TCPCP"))
        {
            return NhcAdvisoryKind.Intermediate;
        }

        if (ContainsAny(content, "TWDAT", "TWDEP"))
        {
            return NhcAdvisoryKind.TropicalWeatherDiscussion;
        }

        return NhcAdvisoryKind.Unknown;
    }

    public static NhcRegion DetectRegion(string content)
    {
        if (ContainsAny(content, "TCMAT", "TCPAT"))
        {
            return NhcRegion.Atlantic;
        }

        if (ContainsAny(content, "TCMEP", "TCPEP"))
        {
            return NhcRegion.EasternPacific;
        }

        if (ContainsAny(content, "TCMCP", "TCPCP"))
        {
            return NhcRegion.CentralPacific;
        }

        return NhcRegion.Unknown;
    }

    public static bool IsFinalAdvisory(string content) =>
        content.Contains("THIS IS THE LAST FORECAST/ADVISORY", StringComparison.OrdinalIgnoreCase) ||
        content.Contains("THE LAST PUBLIC ADVISORY", StringComparison.OrdinalIgnoreCase);

    public static DateTimeOffset ParseIssuedAtUtc(string content)
    {
        var collapsed = CollapseWhitespace(content);

        var twelveHourMatch = TwelveHourDateRegex().Match(collapsed);
        if (twelveHourMatch.Success)
        {
            var time = twelveHourMatch.Groups["time"].Value;
            var paddedTime = time.Length == 3 ? $"0{time}" : time;
            var hour = int.Parse(paddedTime[..^2], CultureInfo.InvariantCulture);
            var minute = int.Parse(paddedTime[^2..], CultureInfo.InvariantCulture);

            if (string.Equals(twelveHourMatch.Groups["ampm"].Value, "PM", StringComparison.OrdinalIgnoreCase) && hour < 12)
            {
                hour += 12;
            }
            else if (string.Equals(twelveHourMatch.Groups["ampm"].Value, "AM", StringComparison.OrdinalIgnoreCase) && hour == 12)
            {
                hour = 0;
            }

            return ParseIssuedAtUtc(
                hour,
                minute,
                twelveHourMatch.Groups["tz"].Value,
                twelveHourMatch.Groups["month"].Value,
                twelveHourMatch.Groups["day"].Value,
                twelveHourMatch.Groups["year"].Value);
        }

        var twentyFourHourMatch = TwentyFourHourDateRegex().Match(collapsed);
        if (twentyFourHourMatch.Success)
        {
            var time = twentyFourHourMatch.Groups["time"].Value;
            var hour = int.Parse(time[..^2], CultureInfo.InvariantCulture);
            var minute = int.Parse(time[^2..], CultureInfo.InvariantCulture);
            return ParseIssuedAtUtc(
                hour,
                minute,
                twentyFourHourMatch.Groups["tz"].Value,
                twentyFourHourMatch.Groups["month"].Value,
                twentyFourHourMatch.Groups["day"].Value,
                twentyFourHourMatch.Groups["year"].Value);
        }

        throw new InvalidOperationException("Could not parse advisory issue time.");
    }

    public static int ParseStormNumber(string content)
    {
        var match = StormNumberRegex().Match(CollapseWhitespace(content));
        if (!match.Success)
        {
            throw new InvalidOperationException("Could not parse storm number.");
        }

        return int.Parse(match.Groups["stormNumber"].Value, CultureInfo.InvariantCulture);
    }

    public static string ParseAdvisoryNumber(string content)
    {
        var match = AdvisoryNumberRegex().Match(CollapseWhitespace(content));
        if (!match.Success)
        {
            throw new InvalidOperationException("Could not parse advisory number.");
        }

        return match.Groups["advisoryNumber"].Value.Trim();
    }

    public static string ParseStormName(string content, IReadOnlyCollection<string>? validNames = null)
    {
        if (TryParseStormNameUsingValidNames(content, validNames, out var validName))
        {
            return validName;
        }

        if (validNames is not null)
        {
            throw new InvalidOperationException("Storm name was not found in the valid names list.");
        }

        var collapsed = CollapseWhitespace(content);
        var match = AdvisoryTitleRegex().Match(collapsed);
        if (match.Success)
        {
            return ToTitleCaseStormName(match.Groups["advisoryTitle"].Value.Trim());
        }

        if (TryParseStormNameFromTitle(content, out var titleName))
        {
            return titleName;
        }

        throw new InvalidOperationException("Could not parse storm name.");
    }

    public static double ParseCoordinate(string value, string hemisphere)
    {
        var coordinate = double.Parse(value, CultureInfo.InvariantCulture);
        if (string.Equals(hemisphere, "S", StringComparison.OrdinalIgnoreCase) || string.Equals(hemisphere, "W", StringComparison.OrdinalIgnoreCase))
        {
            coordinate *= -1;
        }

        return coordinate;
    }

    public static int KtsToMph(int knots) => (int)((knots * 1.1516) + 0.5);

    public static int MphToKts(int mph) => (int)Math.Round(mph / 1.1516, MidpointRounding.AwayFromZero);

    public static ParsedStormType DeriveStormType(int? windSpeedMph)
    {
        if (!windSpeedMph.HasValue)
        {
            return ParsedStormType.Unknown;
        }

        return windSpeedMph.Value switch
        {
            > 0 and <= 38 => ParsedStormType.TropicalDepression,
            >= 39 and <= 73 => ParsedStormType.TropicalStorm,
            >= 74 => ParsedStormType.Hurricane,
            _ => ParsedStormType.Unknown,
        };
    }

    public static int DegreesToDirectionCode(int degrees)
    {
        var value = Convert.ToDouble(degrees, CultureInfo.InvariantCulture);

        if (value >= 0.0 && value < 11.25) return 1;
        if (value >= 11.25 && value < 33.75) return 2;
        if (value >= 33.75 && value < 56.25) return 3;
        if (value >= 56.25 && value < 78.75) return 4;
        if (value >= 78.75 && value < 101.25) return 5;
        if (value >= 101.25 && value < 123.75) return 6;
        if (value >= 123.75 && value < 146.25) return 7;
        if (value >= 146.25 && value < 168.75) return 8;
        if (value >= 168.75 && value < 191.25) return 9;
        if (value >= 191.25 && value < 213.75) return 10;
        if (value >= 213.75 && value < 236.25) return 11;
        if (value >= 236.25 && value < 258.75) return 12;
        if (value >= 258.75 && value < 281.25) return 13;
        if (value >= 281.25 && value < 303.75) return 14;
        if (value >= 303.75 && value < 326.75) return 15;
        if (value >= 326.75 && value < 348.75) return 16;
        if (value >= 348.75 && value <= 360.0) return 1;

        return 0;
    }

    public static int HeadingToDirectionCode(string heading)
    {
        return heading.Trim().ToUpperInvariant() switch
        {
            "N" => 1,
            "NNE" => 2,
            "NE" => 3,
            "ENE" => 4,
            "E" => 5,
            "ESE" => 6,
            "SE" => 7,
            "SSE" => 8,
            "S" => 9,
            "SSW" => 10,
            "SW" => 11,
            "WSW" => 12,
            "W" => 13,
            "WNW" => 14,
            "NW" => 15,
            "NNW" => 16,
            _ => 0,
        };
    }

    public static DateTimeOffset BuildForecastDate(DateTimeOffset issuedAtUtc, int day, string hhmm)
    {
        var hour = int.Parse(hhmm[..^2], CultureInfo.InvariantCulture);
        var minute = int.Parse(hhmm[^2..], CultureInfo.InvariantCulture);
        var forecastMonth = issuedAtUtc.Month;
        var forecastYear = issuedAtUtc.Year;

        if (day < issuedAtUtc.Day)
        {
            var nextMonth = issuedAtUtc.AddMonths(1);
            forecastMonth = nextMonth.Month;
            forecastYear = nextMonth.Year;
        }

        return new DateTimeOffset(forecastYear, forecastMonth, day, hour, minute, 0, TimeSpan.Zero);
    }

    private static DateTimeOffset ParseIssuedAtUtc(int hour, int minute, string timeZone, string month, string day, string year)
    {
        var localTime = new DateTimeOffset(
            int.Parse(year, CultureInfo.InvariantCulture),
            MonthFromAbbreviation(month),
            int.Parse(day, CultureInfo.InvariantCulture),
            hour,
            minute,
            0,
            TimeSpan.Zero);

        if (!TimeZoneOffsets.TryGetValue(timeZone, out var offsetHours))
        {
            throw new InvalidOperationException($"Unsupported advisory time zone '{timeZone}'.");
        }

        return localTime.AddHours(offsetHours);
    }

    private static int MonthFromAbbreviation(string month) => month.Trim().ToUpperInvariant() switch
    {
        "JAN" => 1,
        "FEB" => 2,
        "MAR" => 3,
        "APR" => 4,
        "MAY" => 5,
        "JUN" => 6,
        "JUL" => 7,
        "AUG" => 8,
        "SEP" => 9,
        "OCT" => 10,
        "NOV" => 11,
        "DEC" => 12,
        _ => throw new InvalidOperationException($"Unknown month token '{month}'."),
    };

    private static bool ContainsAny(string content, params string[] values) => values.Any(value => content.Contains(value, StringComparison.OrdinalIgnoreCase));

    private static bool TryParseStormNameUsingValidNames(string content, IReadOnlyCollection<string>? validNames, out string stormName)
    {
        stormName = string.Empty;

        if (validNames is null || validNames.Count == 0)
        {
            return false;
        }

        var validNameSet = new HashSet<string>(
            validNames.Where(static name => !string.IsNullOrWhiteSpace(name)).Select(static name => name.Trim()),
            StringComparer.OrdinalIgnoreCase);

        foreach (Match tokenMatch in AdvisoryTitleTokenRegex().Matches(GetAdvisoryTitlePrefix(content)))
        {
            var token = tokenMatch.Value.Trim();
            if (!validNameSet.Contains(token))
            {
                continue;
            }

            var normalized = WordToNumber(token);
            stormName = normalized.All(char.IsDigit) ? normalized : ToTitleCaseStormName(normalized);
            return true;
        }

        return false;
    }

    private static bool TryParseStormNameFromTitle(string content, out string stormName)
    {
        stormName = string.Empty;

        var candidates = AdvisoryTitleTokenRegex()
            .Matches(GetAdvisoryTitlePrefix(content))
            .Select(static match => match.Value.Trim())
            .Where(static token => !AdvisoryTitleNoiseWords.Contains(token))
            .Where(static token => StormNameTokenRegex().IsMatch(token))
            .ToArray();

        if (candidates.Length == 0)
        {
            return false;
        }

        var normalized = WordToNumber(candidates[^1]);
        stormName = normalized.All(char.IsDigit) ? normalized : ToTitleCaseStormName(normalized);
        return true;
    }

    private static string GetAdvisoryTitlePrefix(string content)
    {
        var upperContent = content.ToUpperInvariant();
        var advisoryNumberIndex = upperContent.IndexOf(AdvisoryNumberSearchText, StringComparison.Ordinal);
        if (advisoryNumberIndex >= 0)
        {
            return upperContent[..(advisoryNumberIndex + AdvisoryNumberSearchText.Length)];
        }

        return upperContent;
    }

    private static string WordToNumber(string word)
    {
        return word.ToUpperInvariant() switch
        {
            "ONE" or "ONE-E" => "01",
            "TWO" or "TWO-E" => "02",
            "THREE" or "THREE-E" => "03",
            "FOUR" or "FOUR-E" => "04",
            "FIVE" or "FIVE-E" => "05",
            "SIX" or "SIX-E" => "06",
            "SEVEN" or "SEVEN-E" => "07",
            "EIGHT" or "EIGHT-E" => "08",
            "NINE" or "NINE-E" => "09",
            "TEN" or "TEN-E" => "10",
            "ELEVEN" or "ELEVEN-E" => "11",
            "TWELEVE" or "TWELEVE-E" => "12",
            _ => word,
        };
    }

    private static string ToTitleCaseStormName(string value)
    {
        var words = value.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return string.Join(
            ' ',
            words.Select(word => string.Join('-', word.Split('-', StringSplitOptions.RemoveEmptyEntries).Select(TitleCaseToken))));
    }

    private static string TitleCaseToken(string token)
    {
        if (token.Length == 0)
        {
            return token;
        }

        if (token.All(char.IsDigit))
        {
            return token;
        }

        return char.ToUpperInvariant(token[0]) + token[1..].ToLowerInvariant();
    }

    [GeneratedRegex(@"\s+", RegexOptions.Compiled)]
    private static partial Regex SpaceRegex();

    [GeneratedRegex(@"\b(?<time>\d{3,4})\s*(?<ampm>AM|PM)\s+(?<tz>GMT|UTC|ADT|AST|EDT|EST|CDT|CST|PDT|PST|HDT|HST)\s+\w{3}\s+(?<month>[A-Z]{3})\s+(?<day>\d{1,2})\s+(?<year>\d{4})\b", RegexOptions.IgnoreCase | RegexOptions.Compiled)]
    private static partial Regex TwelveHourDateRegex();

    [GeneratedRegex(@"\b(?<time>\d{4})\s+(?<tz>GMT|UTC|ADT|AST|EDT|EST|CDT|CST|PDT|PST|HDT|HST)\s+\w{3}\s+(?<month>[A-Z]{3})\s+(?<day>\d{1,2})\s+(?<year>\d{4})\b", RegexOptions.IgnoreCase | RegexOptions.Compiled)]
    private static partial Regex TwentyFourHourDateRegex();

    [GeneratedRegex(@"\b(?:AL|EP|CP)(?<stormNumber>\d{2})\d{4}\b", RegexOptions.IgnoreCase | RegexOptions.Compiled)]
    private static partial Regex StormNumberRegex();

    [GeneratedRegex(@"ADVISORY\s+NUMBER\s+(?<advisoryNumber>[0-9A-Z.]+)", RegexOptions.IgnoreCase | RegexOptions.Compiled)]
    private static partial Regex AdvisoryNumberRegex();

    [GeneratedRegex(@"(?:POST-TROPICAL CYCLONE|POTENTIAL TROPICAL CYCLONE|SUBTROPICAL STORM|SUBTROPICAL DEPRESSION|TROPICAL STORM|TROPICAL DEPRESSION|HURRICANE)\s+(?<advisoryTitle>[A-Z][A-Z0-9-]+(?:\s+[A-Z][A-Z0-9-]+)*)\s+(?:FORECAST/ADVISORY|ADVISORY)\s+NUMBER\b", RegexOptions.IgnoreCase | RegexOptions.Compiled)]
    private static partial Regex AdvisoryTitleRegex();

    [GeneratedRegex(@"^[A-Z0-9-]+$", RegexOptions.IgnoreCase | RegexOptions.Compiled)]
    private static partial Regex StormNameTokenRegex();

    [GeneratedRegex(@"[A-Z0-9-]+", RegexOptions.Compiled)]
    private static partial Regex AdvisoryTitleTokenRegex();
}