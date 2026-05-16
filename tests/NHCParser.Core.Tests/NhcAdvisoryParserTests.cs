using NHCParser.Core.Models;
using NHCParser.Core.Services;
using Xunit;

namespace NHCParser.Core.Tests;

public sealed class NhcAdvisoryParserTests
{
    private readonly NhcAdvisoryParser parser = new();
    private readonly NhcTropicalWeatherDiscussionParser tropicalWeatherDiscussionParser = new();

    [Fact]
    public void Parse_NormalAdvisory_ExtractsCurrentValuesAndForecasts()
    {
        var advisory = parser.Parse(
            """
            WTPZ25 KNHC 091437
            TCMEP5

            POST-TROPICAL CYCLONE OCTAVE FORECAST/ADVISORY
            NUMBER  38
            NWS NATIONAL HURRICANE CENTER MIAMI FL       EP152025
            1500 UTC THU OCT 09 2025

            POST-TROPICAL CYCLONE CENTER LOCATED NEAR 17.7N 110.5W AT
            09/1500Z
            POSITION ACCURATE WITHIN  20 NM

            PRESENT MOVEMENT TOWARD THE EAST-NORTHEAST OR  65
            DEGREES AT  15 KT

            ESTIMATED MINIMUM CENTRAL PRESSURE 1006 MB
            MAX SUSTAINED WINDS
             30 KT WITH GUSTS TO  40 KT.

            FORECAST VALID
            10/0000Z...DISSIPATED

            THIS IS THE LAST FORECAST/ADVISORY ISSUED BY THE NATIONAL HURRICANE
            CENTER ON THIS SYSTEM.
            """);

        Assert.Equal(NhcAdvisoryKind.Normal, advisory.Kind);
        Assert.Equal(NhcRegion.EasternPacific, advisory.Region);
        Assert.Equal("Octave", advisory.StormName);
        Assert.Equal(15, advisory.StormNumber);
        Assert.Equal("38", advisory.AdvisoryNumber);
        Assert.Equal(new DateTimeOffset(2025, 10, 9, 15, 0, 0, TimeSpan.Zero), advisory.IssuedAtUtc);
        Assert.True(advisory.IsFinalAdvisory);

        Assert.Equal(17.7d, advisory.Current.Latitude, 3);
        Assert.Equal(-110.5d, advisory.Current.Longitude, 3);
        Assert.Equal(1006, advisory.Current.PressureMb);
        Assert.Equal(30, advisory.Current.WindSpeedKts);
        Assert.Equal(35, advisory.Current.WindSpeedMph);
        Assert.Equal(65, advisory.Current.MovementDirectionDegrees);
        Assert.Equal(4, advisory.Current.MovementDirectionCode);
        Assert.Equal("EAST-NORTHEAST", advisory.Current.MovementHeading);
        Assert.Equal(15, advisory.Current.MovementSpeedKts);
        Assert.Equal(17, advisory.Current.MovementSpeedMph);
        Assert.Equal(ParsedStormType.TropicalDepression, advisory.Current.StormType);

        var forecastPoint = Assert.Single(advisory.ForecastPoints);
        Assert.True(forecastPoint.IsDissipated);
        Assert.Equal(new DateTimeOffset(2025, 10, 10, 0, 0, 0, TimeSpan.Zero), forecastPoint.ValidAtUtc);
    }

    [Fact]
    public void Parse_IntermediateAdvisory_ExtractsSummaryValues()
    {
        var advisory = parser.Parse(
            """
            WTNT31 KNHC 102031
            TCPAT1

            BULLETIN
            Post-Tropical Cyclone Karen Advisory Number
              4
            NWS National Hurricane Center Miami FL       AL112025
            900 PM GMT Fri Oct 10 2025

            SUMMARY OF 900 PM GMT...2100
            UTC...INFORMATION
            ----------------------------------------------
            LOCATION...47.5N 30.2W
            ABOUT 675 MI...1085 KM NNW OF THE
            AZORES
            MAXIMUM SUSTAINED WINDS...45 MPH...75 KM/H
            PRESENT MOVEMENT...NNE OR 20 DEGREES
            AT 16 MPH...26 KM/H
            MINIMUM CENTRAL PRESSURE...1000 MB...29.53 INCHES

            NEXT
            ADVISORY
            -------------
            This is the last public advisory issued by the National Hurricane
            Center on Karen.
            """,
            ["Karen"]);

        Assert.Equal(NhcAdvisoryKind.Intermediate, advisory.Kind);
        Assert.Equal(NhcRegion.Atlantic, advisory.Region);
        Assert.Equal("Karen", advisory.StormName);
        Assert.Equal(11, advisory.StormNumber);
        Assert.Equal("4", advisory.AdvisoryNumber);
        Assert.Equal(new DateTimeOffset(2025, 10, 10, 21, 0, 0, TimeSpan.Zero), advisory.IssuedAtUtc);
        Assert.True(advisory.IsFinalAdvisory);

        Assert.Equal(47.5d, advisory.Current.Latitude, 3);
        Assert.Equal(-30.2d, advisory.Current.Longitude, 3);
        Assert.Equal(1000, advisory.Current.PressureMb);
        Assert.Equal(45, advisory.Current.WindSpeedMph);
        Assert.Equal(39, advisory.Current.WindSpeedKts);
        Assert.Equal(20, advisory.Current.MovementDirectionDegrees);
        Assert.Equal(2, advisory.Current.MovementDirectionCode);
        Assert.Equal("NNE", advisory.Current.MovementHeading);
        Assert.Equal(16, advisory.Current.MovementSpeedMph);
        Assert.Equal(14, advisory.Current.MovementSpeedKts);
        Assert.Equal(ParsedStormType.TropicalStorm, advisory.Current.StormType);
        Assert.Empty(advisory.ForecastPoints);
    }

    [Fact]
    public void Parse_IntermediateAdvisory_RemnantsOf_ExtractsStormName()
    {
        var advisory = parser.Parse(
            """
            WTNT32 KNHC 152031
            TCPAT2

            BULLETIN
            Remnants Of Lorenzo Advisory Number  11
            NWS National Hurricane Center Miami FL       AL122025
            500 PM AST Wed Oct 15 2025

            SUMMARY OF 500 PM AST...2100 UTC...INFORMATION
            ----------------------------------------------
            LOCATION...23.1N 42.5W
            MAXIMUM SUSTAINED WINDS...35 MPH...55 KM/H
            PRESENT MOVEMENT...NE OR 35 DEGREES AT 18 MPH...30 KM/H
            MINIMUM CENTRAL PRESSURE...1006 MB...29.71 INCHES

            NEXT ADVISORY
            -------------
            This is the last public advisory issued by the National Hurricane Center on this system.
            """,
            ["Lorenzo", "Karen", "Priscilla"]);

        Assert.Equal(NhcAdvisoryKind.Intermediate, advisory.Kind);
        Assert.Equal(NhcRegion.Atlantic, advisory.Region);
        Assert.Equal("Lorenzo", advisory.StormName);
        Assert.Equal(12, advisory.StormNumber);
        Assert.Equal("11", advisory.AdvisoryNumber);
        Assert.Equal(new DateTimeOffset(2025, 10, 15, 21, 0, 0, TimeSpan.Zero), advisory.IssuedAtUtc);
        Assert.True(advisory.IsFinalAdvisory);
    }

    [Fact]
    public void Parse_IntermediateAdvisory_WithMissingValidName_Throws()
    {
        var exception = Assert.Throws<InvalidOperationException>(() => parser.Parse(
            """
            WTNT32 KNHC 152031
            TCPAT2

            BULLETIN
            Experimental System Lorenzo Advisory Number  11
            NWS National Hurricane Center Miami FL       AL122025
            500 PM AST Wed Oct 15 2025

            SUMMARY OF 500 PM AST...2100 UTC...INFORMATION
            ----------------------------------------------
            LOCATION...23.1N 42.5W
            MAXIMUM SUSTAINED WINDS...35 MPH...55 KM/H
            PRESENT MOVEMENT...NE OR 35 DEGREES AT 18 MPH...30 KM/H
            MINIMUM CENTRAL PRESSURE...1006 MB...29.71 INCHES
            """,
            ["Karen", "Priscilla"]));

        Assert.Equal("Storm name was not found in the valid names list.", exception.Message);
    }

    [Fact]
    public void Parse_TropicalWeatherDiscussion_ExtractsWavesAndPoints()
    {
        var discussion = tropicalWeatherDiscussionParser.Parse(
            """
            AXNT20 KNHC 101200
            TWDAT

            TROPICAL WEATHER DISCUSSION

            TROPICAL WAVE ALONG 50W/51W S OF 20N MOVING W 10 KT.
            ...ITCZ...

            GULF OF MEXICO
            A SURFACE LOW IS NEAR 22N90W WITH SCATTERED SHOWERS.

            CARIBBEAN SEA
            A TROUGH IS NEAR 15N75W WITH CONVECTION.

            ATLANTIC OCEAN
            ANOTHER LOW IS NEAR 30N60W.
            """);

        Assert.Equal(5, discussion.PointsOfInterest.Count);

        Assert.Collection(
            discussion.PointsOfInterest,
            first =>
            {
                Assert.Equal(101, first.SequenceAdded);
                Assert.Equal(ParsedPointOfInterestType.TropicalWave, first.Type);
                Assert.Equal(20d, first.Latitude, 3);
                Assert.Equal(-50d, first.Longitude, 3);
            },
            second =>
            {
                Assert.Equal(102, second.SequenceAdded);
                Assert.Equal(ParsedPointOfInterestType.TropicalWave, second.Type);
                Assert.Equal(20d, second.Latitude, 3);
                Assert.Equal(-51d, second.Longitude, 3);
            },
            third => Assert.Equal(ParsedPointOfInterestType.PointOfInterest, third.Type),
            fourth => Assert.Equal(ParsedPointOfInterestType.PointOfInterest, fourth.Type),
            fifth => Assert.Equal(ParsedPointOfInterestType.PointOfInterest, fifth.Type));
    }

    [Fact]
    public void Parse_NormalAdvisory_ExtractsForecastCoordinatePoints()
    {
        var advisory = parser.Parse(
            """
            WTNT21 KNHC 102031
            TCMAT1

            POST-TROPICAL CYCLONE KAREN FORECAST/ADVISORY NUMBER
              4
            NWS NATIONAL HURRICANE CENTER MIAMI FL       AL112025
            2100 UTC FRI OCT 10 2025

            POST-TROPICAL CYCLONE CENTER LOCATED NEAR 47.5N  30.2W AT
            10/2100Z
            POSITION ACCURATE WITHIN  25 NM

            PRESENT MOVEMENT TOWARD THE NORTH-NORTHEAST OR  20
            DEGREES AT  14 KT

            ESTIMATED MINIMUM CENTRAL PRESSURE 1000 MB
            MAX SUSTAINED WINDS
             40 KT WITH GUSTS TO  50 KT.

            FORECAST VALID 11/0600Z 49.4N  29.0W...POST-TROPICAL
            MAX WIND  35 KT...GUSTS  45 KT.

            FORECAST VALID
            11/1800Z 53.5N  27.1W...POST-TROPICAL
            MAX WIND  30 KT...GUSTS  40 KT.

            FORECAST
            VALID 12/0600Z...DISSIPATED
            """,
            ["Karen"]);

        Assert.Equal(3, advisory.ForecastPoints.Count);

        Assert.Collection(
            advisory.ForecastPoints,
            first =>
            {
                Assert.False(first.IsDissipated);
                Assert.True(first.Latitude.HasValue);
                Assert.True(first.Longitude.HasValue);
                Assert.InRange(first.Latitude.Value, 49.399d, 49.401d);
                Assert.InRange(first.Longitude.Value, -29.001d, -28.999d);
                Assert.Equal(35, first.WindSpeedKts);
                Assert.Equal(40, first.WindSpeedMph);
            },
            second =>
            {
                Assert.False(second.IsDissipated);
                Assert.True(second.Latitude.HasValue);
                Assert.True(second.Longitude.HasValue);
                Assert.InRange(second.Latitude.Value, 53.499d, 53.501d);
                Assert.InRange(second.Longitude.Value, -27.101d, -27.099d);
                Assert.Equal(30, second.WindSpeedKts);
                Assert.Equal(35, second.WindSpeedMph);
            },
            third => Assert.True(third.IsDissipated));
    }

    [Fact]
    public void Parse_NormalFinalAdvisory_WithoutForecasts_ReturnsEmptyForecastPoints()
    {
        var advisory = parser.Parse(
            """
            WTPZ31 KNHC 102033
            TCMEP1

            POST-TROPICAL CYCLONE PRISCILLA FORECAST/ADVISORY NUMBER 25
            NWS NATIONAL HURRICANE CENTER MIAMI FL       EP162025
            2100 UTC FRI OCT 10 2025

            POST-TROPICAL CYCLONE CENTER LOCATED NEAR 26.3N 115.4W AT
            10/2100Z

            PRESENT MOVEMENT TOWARD THE NORTH OR 350 DEGREES AT 6 KT

            ESTIMATED MINIMUM CENTRAL PRESSURE 1004 MB
            MAX SUSTAINED WINDS 30 KT WITH GUSTS TO 40 KT.

            THIS IS THE LAST FORECAST/ADVISORY ISSUED BY THE NATIONAL HURRICANE
            CENTER ON THIS SYSTEM.
            """);

        Assert.Equal(NhcAdvisoryKind.Normal, advisory.Kind);
        Assert.True(advisory.IsFinalAdvisory);
        Assert.Equal("Priscilla", advisory.StormName);
        Assert.Equal("25", advisory.AdvisoryNumber);
        Assert.Empty(advisory.ForecastPoints);
    }

    [Fact]
    public void Parse_NoaaTcmExample_ExtractsExpectedNormalAdvisoryValues()
    {
        var advisory = parser.Parse(
            """
            WTNT23 KNHC 102156
            TCMAT3

            HURRICANE LEE FORECAST/ADVISORY NUMBER  22
            NWS NATIONAL HURRICANE CENTER MIAMI FL       AL132023
            2100 UTC SUN SEP 10 2023

            HURRICANE CENTER LOCATED NEAR 22.1N  61.7W AT 10/2100Z
            POSITION ACCURATE WITHIN  15 NM

            PRESENT MOVEMENT TOWARD THE WEST-NORTHWEST OR 300 DEGREES AT   7 KT

            ESTIMATED MINIMUM CENTRAL PRESSURE  954 MB
            EYE DIAMETER  20 NM
            MAX SUSTAINED WINDS 105 KT WITH GUSTS TO 120 KT.

            FORECAST VALID 11/0600Z 22.7N  62.7W
            MAX WIND 115 KT...GUSTS 140 KT.

            FORECAST VALID 11/1800Z 23.3N  63.9W
            MAX WIND 120 KT...GUSTS 145 KT.

            FORECAST VALID 12/0600Z 23.8N  65.1W
            MAX WIND 120 KT...GUSTS 145 KT.

            FORECAST VALID 12/1800Z 24.2N  66.2W
            MAX WIND 115 KT...GUSTS 140 KT.

            FORECAST VALID 13/0600Z 24.7N  67.0W
            MAX WIND 105 KT...GUSTS 130 KT.

            FORECAST VALID 13/1800Z 25.6N  67.6W
            MAX WIND 100 KT...GUSTS 120 KT.

            OUTLOOK VALID 14/1800Z 28.9N  68.0W
            MAX WIND  90 KT...GUSTS 110 KT.

            OUTLOOK VALID 15/1800Z 33.6N  67.4W
            MAX WIND  80 KT...GUSTS 100 KT.

            NEXT ADVISORY AT 11/0300Z
            """);

        Assert.Equal(NhcAdvisoryKind.Normal, advisory.Kind);
        Assert.Equal(NhcRegion.Atlantic, advisory.Region);
        Assert.Equal("Lee", advisory.StormName);
        Assert.Equal(13, advisory.StormNumber);
        Assert.Equal("22", advisory.AdvisoryNumber);
        Assert.Equal(new DateTimeOffset(2023, 9, 10, 21, 0, 0, TimeSpan.Zero), advisory.IssuedAtUtc);
        Assert.False(advisory.IsFinalAdvisory);

        Assert.Equal(22.1d, advisory.Current.Latitude, 3);
        Assert.Equal(-61.7d, advisory.Current.Longitude, 3);
        Assert.Equal(954, advisory.Current.PressureMb);
        Assert.Equal(105, advisory.Current.WindSpeedKts);
        Assert.Equal(121, advisory.Current.WindSpeedMph);
        Assert.Equal("WEST-NORTHWEST", advisory.Current.MovementHeading);
        Assert.Equal(300, advisory.Current.MovementDirectionDegrees);
        Assert.Equal(14, advisory.Current.MovementDirectionCode);
        Assert.Equal(7, advisory.Current.MovementSpeedKts);
        Assert.Equal(8, advisory.Current.MovementSpeedMph);
        Assert.Equal(ParsedStormType.Hurricane, advisory.Current.StormType);

        Assert.Equal(8, advisory.ForecastPoints.Count);

        Assert.Collection(
            advisory.ForecastPoints.Take(2),
            first =>
            {
                Assert.InRange(first.Latitude!.Value, 22.699d, 22.701d);
                Assert.InRange(first.Longitude!.Value, -62.701d, -62.699d);
                Assert.Equal(115, first.WindSpeedKts);
                Assert.Equal(132, first.WindSpeedMph);
            },
            second =>
            {
                Assert.InRange(second.Latitude!.Value, 23.299d, 23.301d);
                Assert.InRange(second.Longitude!.Value, -63.901d, -63.899d);
                Assert.Equal(120, second.WindSpeedKts);
                Assert.Equal(138, second.WindSpeedMph);
            });
    }
}