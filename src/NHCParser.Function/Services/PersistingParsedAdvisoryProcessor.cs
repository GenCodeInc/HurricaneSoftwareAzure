using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NHCParser.Core.Models;
using NHCParser.Core.Services;
using TTENET.TTEBusiness.Core.Models;
using TTENET.TTEBusiness.Core.Services;

namespace NHCParser.Function.Services;

public sealed class PersistingParsedAdvisoryProcessor(INhcAdvisoryParser advisoryParser, INhcTropicalWeatherDiscussionParser tropicalWeatherDiscussionParser, ITteRepository tteRepository, IOptions<NHCParserOptions> options, ILogger<PersistingParsedAdvisoryProcessor> logger) : INhcAdvisoryProcessor
{
    public async Task ProcessAsync(AdvisoryDocument document, CancellationToken cancellationToken)
    {
        var parserOptions = options.Value;

        if (document.AdvisoryType == TTENET.TTEBusiness.Core.Models.AdvisoryType.TropicalWeatherDiscussion)
        {
            var parsedDiscussion = tropicalWeatherDiscussionParser.Parse(document.Content);
            var pointsOfInterest = parsedDiscussion.PointsOfInterest
                .Select(point => new PersistPointOfInterestRequest
                {
                    Latitude = point.Latitude,
                    Longitude = point.Longitude,
                    Type = (int)point.Type,
                    SequenceAdded = point.SequenceAdded,
                    RelatedText = point.RelatedText,
                })
                .ToArray();

            var persistedPointCount = await tteRepository.ReplacePointsOfInterestAsync(pointsOfInterest, cancellationToken).ConfigureAwait(false);
            logger.LogInformation("Persisted tropical weather discussion points of interest. Count={Count}", persistedPointCount);
            return;
        }

        IReadOnlyList<string>? validNames = null;
        try
        {
            validNames = await tteRepository.GetValidNamesAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex) when (!cancellationToken.IsCancellationRequested)
        {
            logger.LogWarning(ex, "Failed to load valid storm names. Skipping advisory processing for source {SourceName}.", document.Source.Name);
            return;
        }

        var parsed = advisoryParser.Parse(document.Content, validNames);

        logger.LogInformation(
            "Parsed advisory. Source={SourceName}. Kind={Kind}. Storm={StormName}. StormNumber={StormNumber}. AdvisoryNumber={AdvisoryNumber}. Region={Region}. IssuedAtUtc={IssuedAtUtc}. Latitude={Latitude}. Longitude={Longitude}. WindMph={WindMph}. WindKts={WindKts}. PressureMb={PressureMb}. ForecastPoints={ForecastPoints}. Final={IsFinalAdvisory}",
            document.Source.Name,
            parsed.Kind,
            parsed.StormName,
            parsed.StormNumber,
            parsed.AdvisoryNumber,
            parsed.Region,
            parsed.IssuedAtUtc,
            parsed.Current.Latitude,
            parsed.Current.Longitude,
            parsed.Current.WindSpeedMph,
            parsed.Current.WindSpeedKts,
            parsed.Current.PressureMb,
            parsed.ForecastPoints.Count,
            parsed.IsFinalAdvisory);

        var currentUtcYear = DateTimeOffset.UtcNow.Year;
        if (parserOptions.CurrentYearOnly && parsed.IssuedAtUtc.Year != currentUtcYear && !parsed.IsFinalAdvisory)
        {
            logger.LogInformation(
            "Skipping persistence for {StormName} advisory {AdvisoryNumber}. CurrentYearOnly is enabled, the advisory is not final, and advisory year {AdvisoryYear} does not match current UTC year {CurrentYear}.",
                parsed.StormName,
                parsed.AdvisoryNumber,
                parsed.IssuedAtUtc.Year,
                currentUtcYear);
            return;
        }

        if (!parsed.Current.PressureMb.HasValue || !parsed.Current.WindSpeedMph.HasValue || !parsed.Current.MovementSpeedMph.HasValue || !parsed.Current.MovementDirectionCode.HasValue)
        {
            logger.LogWarning(
                "Skipping persistence for {StormName} advisory {AdvisoryNumber} because one or more required current-condition values are missing.",
                parsed.StormName,
                parsed.AdvisoryNumber);
            return;
        }

        var request = new PersistAdvisoryRequest
        {
            StormName = parsed.StormName,
            StormNumber = parsed.StormNumber,
            AdvisoryNumber = parsed.AdvisoryNumber,
            Year = parsed.IssuedAtUtc.Year,
            RegionType = MapRegionType(parsed.Region),
            StormType = MapStormType(parsed.Current.StormType),
            IsFinalAdvisory = parsed.IsFinalAdvisory,
            IssuedAtUtc = parsed.IssuedAtUtc,
            Latitude = parsed.Current.Latitude,
            Longitude = parsed.Current.Longitude,
            WindSpeed = parsed.Current.WindSpeedMph.Value,
            Pressure = parsed.Current.PressureMb.Value,
            SpeedOfTravel = parsed.Current.MovementSpeedMph.Value,
            Direction = parsed.Current.MovementDirectionCode.Value,
            UpdateAdvisorySlot = parsed.Kind == NhcAdvisoryKind.Normal,
        };

        var result = await tteRepository.PersistAdvisoryAsync(request, cancellationToken).ConfigureAwait(false);

        logger.LogInformation(
            "Persistence result. Storm={StormName}. StormId={StormId}. StormCreated={StormCreated}. StormUpdated={StormUpdated}. CoordinateInserted={CoordinateInserted}. AdvisoryRowsUpdated={AdvisoryRowsUpdated}.",
            parsed.StormName,
            result.StormId,
            result.StormCreated,
            result.StormUpdated,
            result.CoordinateInserted,
            result.AdvisoryRowsUpdated);

        if (parsed.Kind == NhcAdvisoryKind.Normal)
        {
            var forecastPoints = parsed.ForecastPoints
                .Where(point => !point.IsDissipated && point.Latitude.HasValue && point.Longitude.HasValue && point.WindSpeedMph.HasValue)
                .Select(point => new PersistForecastPointRequest
                {
                    AdvisoryNumber = parsed.AdvisoryNumber,
                    ValidAtUtc = point.ValidAtUtc,
                    Latitude = point.Latitude!.Value,
                    Longitude = point.Longitude!.Value,
                    WindSpeed = point.WindSpeedMph!.Value,
                })
                .ToArray();

            if (forecastPoints.Length > 0)
            {
                var insertedForecastCount = await tteRepository.PersistForecastAsync(
                    new PersistForecastRequest
                    {
                        StormName = parsed.StormName,
                        StormNumber = parsed.StormNumber,
                        Year = parsed.IssuedAtUtc.Year,
                        RegionType = request.RegionType,
                        ForecastPoints = forecastPoints,
                    },
                    cancellationToken).ConfigureAwait(false);

                logger.LogInformation(
                    "Forecast persistence result. Storm={StormName}. ForecastCoordinatesInserted={ForecastCoordinatesInserted}.",
                    parsed.StormName,
                    insertedForecastCount);
            }
        }
    }

    private static int MapRegionType(NhcRegion region) => region switch
    {
        NhcRegion.Atlantic => 1,
        NhcRegion.EasternPacific => 2,
        NhcRegion.CentralPacific => 2,
        _ => throw new NotSupportedException($"Region '{region}' is not supported for persistence."),
    };

    private static int MapStormType(ParsedStormType stormType) => stormType switch
    {
        ParsedStormType.TropicalDepression => 1,
        ParsedStormType.TropicalStorm => 3,
        ParsedStormType.Hurricane => 4,
        _ => 0,
    };
}