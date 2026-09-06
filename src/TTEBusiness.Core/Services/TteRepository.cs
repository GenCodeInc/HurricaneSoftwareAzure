using System.Data;
using System.Data.Common;
using System.Globalization;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;
using TTENET.TTEBusiness.Core.Models;

namespace TTENET.TTEBusiness.Core.Services;

public sealed class TteRepository(IOptions<NHCParserOptions> options) : ITteRepository
{
    private static readonly TimeSpan FinalAdvisoryActiveWindow = TimeSpan.FromHours(24);
    private static readonly TimeSpan ForecastCoordinateActiveWindow = TimeSpan.FromHours(12);
    private const string StormCenterImageBaseUrl = "https://cdn.star.nesdis.noaa.gov/FLOATER";

    public async Task<IReadOnlyList<AdvisoryRecord>> GetAdvisoriesAsync(int regionType, CancellationToken cancellationToken)
    {
        var connectionString = options.Value.SqlConnectionString;
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException("NHCParser:SqlConnectionString is not configured.");
        }

        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        var rows = await connection.QueryAsync<AdvisoryRecord>(
            new CommandDefinition(
                "dbo.msp_Advisory_Get",
                new { StormID = (int?)null, RegionType = regionType },
                commandType: CommandType.StoredProcedure,
                cancellationToken: cancellationToken)).ConfigureAwait(false);

        return rows.ToList();
    }

    public async Task<IReadOnlyList<string>> GetValidNamesAsync(CancellationToken cancellationToken)
    {
        var connectionString = options.Value.SqlConnectionString;
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException("NHCParser:SqlConnectionString is not configured.");
        }

        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        var rows = await connection.QueryAsync<string>(
            new CommandDefinition(
                "SELECT [Name] FROM dbo.ValidNames ORDER BY [Name];",
                commandType: CommandType.Text,
                cancellationToken: cancellationToken)).ConfigureAwait(false);

        return rows
            .Where(static row => !string.IsNullOrWhiteSpace(row))
            .Select(static row => row.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    public async Task<PersistAdvisoryResult> PersistAdvisoryAsync(PersistAdvisoryRequest request, CancellationToken cancellationToken)
    {
        var connectionString = options.Value.SqlConnectionString;
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException("NHCParser:SqlConnectionString is not configured.");
        }

        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);

        var storms = (await connection.QueryAsync<StormRow>(
            new CommandDefinition(
                "dbo.msp_Storm_Get",
                new
                {
                    StormID = (int?)null,
                    Name = (string?)null,
                    RegionType = request.RegionType,
                    ShowOnTTE = (bool?)null,
                    EmailAlertsSent = (bool?)null,
                    Active = (bool?)null,
                    StormNumber = request.StormNumber,
                    Year = request.Year,
                },
                transaction: transaction,
                commandType: CommandType.StoredProcedure,
                cancellationToken: cancellationToken)).ConfigureAwait(false)).ToList();

        var storm = storms.FirstOrDefault(static row => row.StormType != 0);
        var stormCreated = false;
        var stormUpdated = false;

        if (storm is null)
        {
            storm = await InsertStormAsync(
                connection,
                transaction,
                request.StormName,
                request.Year,
                request.RegionType,
                request.StormType,
                active: true,
                request.StormNumber,
                cancellationToken).ConfigureAwait(false);

            stormCreated = true;
        }
        else if (!string.Equals(storm.Name, request.StormName, StringComparison.OrdinalIgnoreCase))
        {
            await connection.ExecuteAsync(
                new CommandDefinition(
                    "dbo.msp_Storm_Upd",
                    new
                    {
                        ID = storm.StormID,
                        Name = request.StormName,
                        EmailAlertsSent = false,
                        Active = storm.Active,
                        StormNumber = storm.StormNumber,
                    },
                    transaction: transaction,
                    commandType: CommandType.StoredProcedure,
                    cancellationToken: cancellationToken)).ConfigureAwait(false);

            storm = storm with { Name = request.StormName, EmailAlertsSent = false };
            stormUpdated = true;
        }

        var coordinates = (await connection.QueryAsync<CoordinateRow>(
            new CommandDefinition(
                "dbo.msp_Coordinate_Get",
                new
                {
                    StormID = storm.StormID,
                    RegionType = request.RegionType,
                    AdvisoryNumber = request.AdvisoryNumber,
                },
                transaction: transaction,
                commandType: CommandType.StoredProcedure,
                cancellationToken: cancellationToken)).ConfigureAwait(false)).ToList();

        var coordinateInserted = false;
        var advisoryRowsUpdated = 0;

        var desiredActive = storm.Active;
        if (request.IsFinalAdvisory)
        {
            desiredActive = DateTimeOffset.UtcNow - request.IssuedAtUtc <= FinalAdvisoryActiveWindow;
        }
        else if (!storm.Active)
        {
            desiredActive = true;
        }

        if (desiredActive != storm.Active)
        {
            await connection.ExecuteAsync(
                new CommandDefinition(
                    "dbo.msp_Storm_Upd",
                    new
                    {
                        ID = storm.StormID,
                        Name = storm.Name,
                        EmailAlertsSent = storm.EmailAlertsSent,
                        Active = desiredActive,
                        StormNumber = storm.StormNumber,
                    },
                    transaction: transaction,
                    commandType: CommandType.StoredProcedure,
                    cancellationToken: cancellationToken)).ConfigureAwait(false);

            storm = storm with { Active = desiredActive };
            stormUpdated = true;
        }

        if (coordinates.Count == 0)
        {

            await connection.ExecuteAsync(
                new CommandDefinition(
                    "dbo.msp_Coordinate_Ins",
                    new
                    {
                        StormID = storm.StormID,
                        AdvisoryNumber = request.AdvisoryNumber,
                        Latitude = request.Latitude.ToString(CultureInfo.InvariantCulture),
                        Longitude = request.Longitude.ToString(CultureInfo.InvariantCulture),
                        WindSpeed = request.WindSpeed,
                        Pressure = request.Pressure,
                        SpeedOfTravel = request.SpeedOfTravel,
                        Direction = request.Direction,
                        RegionType = request.RegionType,
                        CoordinateType = request.StormType,
                        CoordinateDate = request.IssuedAtUtc.UtcDateTime,
                    },
                    transaction: transaction,
                    commandType: CommandType.StoredProcedure,
                    cancellationToken: cancellationToken)).ConfigureAwait(false);

            coordinateInserted = true;

            if (request.UpdateAdvisorySlot)
            {
                var advisoryRows = (await connection.QueryAsync<AdvisoryRecord>(
                    new CommandDefinition(
                        "dbo.msp_Advisory_Get",
                        new { StormID = (int?)null, RegionType = request.RegionType },
                        transaction: transaction,
                        commandType: CommandType.StoredProcedure,
                        cancellationToken: cancellationToken)).ConfigureAwait(false)).ToList();

                var advisoryIndex = request.StormNumber % 5;
                if (advisoryIndex == 0)
                {
                    advisoryIndex = 5;
                }

                foreach (var advisoryRow in advisoryRows.Where(row => row.AdvisoryIndex == advisoryIndex && row.StormID != storm.StormID))
                {
                    await connection.ExecuteAsync(
                        new CommandDefinition(
                            "dbo.msp_Advisory_Upd",
                            new { ID = advisoryRow.ID, StormID = storm.StormID },
                            transaction: transaction,
                            commandType: CommandType.StoredProcedure,
                            cancellationToken: cancellationToken)).ConfigureAwait(false);

                    advisoryRowsUpdated++;
                }
            }
        }

        stormUpdated = await SyncForecastStormActiveAsync(
            connection,
            transaction,
            storms.FirstOrDefault(static row => row.StormType == 0),
            stormUpdated,
            cancellationToken).ConfigureAwait(false);

        var stormCenterItemAction = "Disabled";
        if (options.Value.SyncStormCenterItems)
        {
            stormCenterItemAction = await SyncStormCenterItemAsync(
                connection,
                transaction,
                request.StormName,
                request.StormIdentifier,
                request.RegionType,
                desiredActive,
                cancellationToken).ConfigureAwait(false);
        }

        await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);

        return new PersistAdvisoryResult
        {
            Skipped = false,
            StormId = storm.StormID,
            StormCreated = stormCreated,
            StormUpdated = stormUpdated,
            CoordinateInserted = coordinateInserted,
            AdvisoryRowsUpdated = advisoryRowsUpdated,
            StormCenterItemAction = stormCenterItemAction,
        };
    }

    public async Task<int> PersistForecastAsync(PersistForecastRequest request, CancellationToken cancellationToken)
    {
        var connectionString = options.Value.SqlConnectionString;
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException("NHCParser:SqlConnectionString is not configured.");
        }

        if (request.ForecastPoints.Count == 0)
        {
            return 0;
        }

        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);

        var storms = (await connection.QueryAsync<StormRow>(
            new CommandDefinition(
                "dbo.msp_Storm_Get",
                new
                {
                    StormID = (int?)null,
                    Name = (string?)null,
                    RegionType = request.RegionType,
                    ShowOnTTE = (bool?)null,
                    EmailAlertsSent = (bool?)null,
                    Active = (bool?)null,
                    StormNumber = request.StormNumber,
                    Year = request.Year,
                },
                transaction: transaction,
                commandType: CommandType.StoredProcedure,
                cancellationToken: cancellationToken)).ConfigureAwait(false)).ToList();

        var mainStorm = storms.FirstOrDefault(static storm => storm.StormType != 0);
        var forecastStorm = storms.FirstOrDefault(static storm => storm.StormType == 0);
        var forecastStormName = request.StormName + "_Forecast";

        if (forecastStorm is null)
        {
            forecastStorm = await InsertStormAsync(
                connection,
                transaction,
                forecastStormName,
                request.Year,
                request.RegionType,
                stormType: 0,
                active: true,
                request.StormNumber,
                cancellationToken).ConfigureAwait(false);
        }
        else if (!string.Equals(forecastStorm.Name, forecastStormName, StringComparison.OrdinalIgnoreCase))
        {
            await connection.ExecuteAsync(
                new CommandDefinition(
                    "dbo.msp_Storm_Upd",
                    new
                    {
                        ID = forecastStorm.StormID,
                        Name = forecastStormName,
                        EmailAlertsSent = true,
                        Active = forecastStorm.Active,
                        StormNumber = forecastStorm.StormNumber,
                    },
                    transaction: transaction,
                    commandType: CommandType.StoredProcedure,
                    cancellationToken: cancellationToken)).ConfigureAwait(false);

            forecastStorm = forecastStorm with { Name = forecastStormName, EmailAlertsSent = true };
        }

        await connection.ExecuteAsync(
            new CommandDefinition(
                "dbo.msp_Coordinate_Del",
                new { StormID = forecastStorm.StormID, RegionType = request.RegionType, CoordinateType = 2 },
                transaction: transaction,
                commandType: CommandType.StoredProcedure,
                cancellationToken: cancellationToken)).ConfigureAwait(false);

        var insertedCount = 0;

        if (mainStorm is not null)
        {
            var mainCoordinates = (await connection.QueryAsync<CoordinateRow>(
                new CommandDefinition(
                    "dbo.msp_Coordinate_Get",
                    new { StormID = mainStorm.StormID, RegionType = (int?)null, AdvisoryNumber = (string?)null },
                    transaction: transaction,
                    commandType: CommandType.StoredProcedure,
                    cancellationToken: cancellationToken)).ConfigureAwait(false)).ToList();

            var lastCoordinate = mainCoordinates.LastOrDefault();
            if (lastCoordinate is not null)
            {
                await connection.ExecuteAsync(
                    new CommandDefinition(
                        "dbo.msp_Coordinate_Ins",
                        new
                        {
                            StormID = forecastStorm.StormID,
                            AdvisoryNumber = lastCoordinate.AdvisoryNumber,
                            Latitude = lastCoordinate.Latitude,
                            Longitude = lastCoordinate.Longitude,
                            WindSpeed = lastCoordinate.WindSpeed,
                            Pressure = lastCoordinate.Pressure,
                            SpeedOfTravel = lastCoordinate.SpeedOfTravel,
                            Direction = lastCoordinate.Direction,
                            RegionType = request.RegionType,
                            CoordinateType = 2,
                            CoordinateDate = lastCoordinate.CoordinateDate,
                        },
                        transaction: transaction,
                        commandType: CommandType.StoredProcedure,
                        cancellationToken: cancellationToken)).ConfigureAwait(false);

                insertedCount++;
            }
        }

        foreach (var point in request.ForecastPoints)
        {
            await connection.ExecuteAsync(
                new CommandDefinition(
                    "dbo.msp_Coordinate_Ins",
                    new
                    {
                        StormID = forecastStorm.StormID,
                        AdvisoryNumber = point.AdvisoryNumber,
                        Latitude = point.Latitude.ToString(CultureInfo.InvariantCulture),
                        Longitude = point.Longitude.ToString(CultureInfo.InvariantCulture),
                        WindSpeed = point.WindSpeed,
                        Pressure = 0,
                        SpeedOfTravel = 0,
                        Direction = 0,
                        RegionType = request.RegionType,
                        CoordinateType = 2,
                        CoordinateDate = point.ValidAtUtc.UtcDateTime,
                    },
                    transaction: transaction,
                    commandType: CommandType.StoredProcedure,
                    cancellationToken: cancellationToken)).ConfigureAwait(false);

            insertedCount++;
        }

        await SyncForecastStormActiveAsync(
            connection,
            transaction,
            forecastStorm,
            stormUpdated: false,
            cancellationToken).ConfigureAwait(false);

        await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
        return insertedCount;
    }

    public async Task<int> DeactivateExpiredForecastsAsync(CancellationToken cancellationToken)
    {
        var connectionString = options.Value.SqlConnectionString;
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException("NHCParser:SqlConnectionString is not configured.");
        }

        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        return await connection.ExecuteAsync(
            new CommandDefinition(
                @"
WITH LatestForecastCoordinates AS (
    SELECT c.StormID, MAX(c.CoordinateDate) AS LastCoordinateDate
    FROM dbo.Coordinate c
    WHERE c.CoordinateType = 2
    GROUP BY c.StormID
)
UPDATE s
SET s.Active = 0
FROM dbo.Storm s
LEFT JOIN LatestForecastCoordinates lfc ON lfc.StormID = s.StormID
WHERE s.StormType = 0
  AND s.Active = 1
  AND (
      lfc.LastCoordinateDate IS NULL
      OR DATEADD(hour, 12, lfc.LastCoordinateDate) <= SYSUTCDATETIME()
  );",
                commandType: CommandType.Text,
                cancellationToken: cancellationToken)).ConfigureAwait(false);
    }

    public async Task<int> RemoveInactiveStormCenterItemsAsync(CancellationToken cancellationToken)
    {
        var connectionString = options.Value.SqlConnectionString;
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException("NHCParser:SqlConnectionString is not configured.");
        }

        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);

        var activeStorms = (await connection.QueryAsync<ActiveStormIdentityRow>(
            new CommandDefinition(
                @"
SELECT StormNumber, [Year], RegionType
FROM dbo.Storm
WHERE Active = 1
  AND StormType <> 0;",
                transaction: transaction,
                cancellationToken: cancellationToken)).ConfigureAwait(false)).ToList();

        var activeIdentifiers = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var storm in activeStorms)
        {
            if (storm.RegionType == 1)
            {
                activeIdentifiers.Add($"AL{storm.StormNumber:D2}{storm.Year:D4}");
            }
            else if (storm.RegionType == 2)
            {
                activeIdentifiers.Add($"EP{storm.StormNumber:D2}{storm.Year:D4}");
                activeIdentifiers.Add($"CP{storm.StormNumber:D2}{storm.Year:D4}");
            }
        }

        var stormCenterItems = (await connection.QueryAsync<MobileTabItemRow>(
            new CommandDefinition(
                @"
SELECT MobileTabItemsID, MobileTabGroupID, URL, Text, ItemType
FROM dbo.MobileTabItems
WHERE MobileTabGroupID IN (1, 2)
  AND URL LIKE @UrlPrefix;",
                new { UrlPrefix = $"{StormCenterImageBaseUrl}/%" },
                transaction: transaction,
                cancellationToken: cancellationToken)).ConfigureAwait(false)).ToList();

        var inactiveItems = stormCenterItems
            .Select(item => new { Item = item, Identifier = TryGetStormIdentifierFromUrl(item.URL) })
            .Where(item => item.Identifier is not null && !activeIdentifiers.Contains(item.Identifier))
            .Select(item => item.Item)
            .ToArray();

        if (inactiveItems.Length > 0)
        {
            await DeleteMobileTabItemsAsync(connection, transaction, inactiveItems, cancellationToken).ConfigureAwait(false);
        }

        await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
        return inactiveItems.Length;
    }

    public async Task<int> ReplacePointsOfInterestAsync(IReadOnlyList<PersistPointOfInterestRequest> requests, CancellationToken cancellationToken)
    {
        var connectionString = options.Value.SqlConnectionString;
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException("NHCParser:SqlConnectionString is not configured.");
        }

        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);

        await connection.ExecuteAsync(
            new CommandDefinition(
                "dbo.msp_PointsOfInterest_Purge",
                transaction: transaction,
                commandType: CommandType.StoredProcedure,
                cancellationToken: cancellationToken)).ConfigureAwait(false);

        foreach (var request in requests.OrderBy(request => request.SequenceAdded))
        {
            await connection.ExecuteAsync(
                new CommandDefinition(
                    "dbo.msp_PointsOfInterest_Ins",
                    new
                    {
                        Latitude = request.Latitude.ToString(CultureInfo.InvariantCulture),
                        Longitude = request.Longitude.ToString(CultureInfo.InvariantCulture),
                        Type = request.Type,
                        SequenceAdded = request.SequenceAdded,
                        RelatedText = request.RelatedText,
                    },
                    transaction: transaction,
                    commandType: CommandType.StoredProcedure,
                    cancellationToken: cancellationToken)).ConfigureAwait(false);
        }

        await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
        return requests.Count;
    }

    private static async Task<bool> SyncForecastStormActiveAsync(
        SqlConnection connection,
        DbTransaction transaction,
        StormRow? forecastStorm,
        bool stormUpdated,
        CancellationToken cancellationToken)
    {
        if (forecastStorm is null)
        {
            return stormUpdated;
        }

        var latestForecastCoordinateUtc = await connection.QuerySingleOrDefaultAsync<DateTime?>(
            new CommandDefinition(
                "SELECT MAX(CoordinateDate) FROM dbo.Coordinate WHERE StormID = @StormID AND CoordinateType = 2",
                new { StormID = forecastStorm.StormID },
                transaction: transaction,
                cancellationToken: cancellationToken)).ConfigureAwait(false);

        var desiredActive = latestForecastCoordinateUtc.HasValue &&
            latestForecastCoordinateUtc.Value.Add(ForecastCoordinateActiveWindow) > DateTime.UtcNow;

        if (forecastStorm.Active == desiredActive)
        {
            return stormUpdated;
        }

        await connection.ExecuteAsync(
            new CommandDefinition(
                "dbo.msp_Storm_Upd",
                new
                {
                    ID = forecastStorm.StormID,
                    Name = forecastStorm.Name,
                    EmailAlertsSent = forecastStorm.EmailAlertsSent,
                    Active = desiredActive,
                    StormNumber = forecastStorm.StormNumber,
                },
                transaction: transaction,
                commandType: CommandType.StoredProcedure,
                cancellationToken: cancellationToken)).ConfigureAwait(false);

        return true;
    }

    private static async Task<string> SyncStormCenterItemAsync(
        SqlConnection connection,
        DbTransaction transaction,
        string stormName,
        string stormIdentifier,
        int regionType,
        bool active,
        CancellationToken cancellationToken)
    {
        var groupId = regionType switch
        {
            1 => 1,
            2 => 2,
            _ => throw new NotSupportedException($"Region type '{regionType}' does not have a storm-center mobile group."),
        };

        var url = $"{StormCenterImageBaseUrl}/{stormIdentifier}/GEOCOLOR/{stormIdentifier}-GEOCOLOR-1000x1000.gif";
        var urlIdentifier = $"/FLOATER/{stormIdentifier}/";
        var existingItems = (await connection.QueryAsync<MobileTabItemRow>(
            new CommandDefinition(
                @"
SELECT MobileTabItemsID, MobileTabGroupID, URL, Text, ItemType
FROM dbo.MobileTabItems
WHERE MobileTabGroupID IN (1, 2)
  AND CHARINDEX(@UrlIdentifier, URL) > 0
ORDER BY MobileTabItemsID;",
                new { UrlIdentifier = urlIdentifier },
                transaction: transaction,
                cancellationToken: cancellationToken)).ConfigureAwait(false)).ToList();

        if (!active)
        {
            if (existingItems.Count == 0)
            {
                return "Unchanged";
            }

            await DeleteMobileTabItemsAsync(connection, transaction, existingItems, cancellationToken).ConfigureAwait(false);
            return "Removed";
        }

        var text = $"{stormName} Center";
        var existingItem = existingItems.FirstOrDefault();
        if (existingItem is null)
        {
            await connection.ExecuteAsync(
                new CommandDefinition(
                    @"
INSERT INTO dbo.MobileTabItems (MobileTabGroupID, URL, Text, ItemType)
VALUES (@GroupId, @Url, @Text, 1);",
                    new { GroupId = groupId, Url = url, Text = text },
                    transaction: transaction,
                    cancellationToken: cancellationToken)).ConfigureAwait(false);

            return "Added";
        }

        var needsUpdate = existingItem.MobileTabGroupID != groupId
            || !string.Equals(existingItem.URL, url, StringComparison.OrdinalIgnoreCase)
            || !string.Equals(existingItem.Text, text, StringComparison.Ordinal)
            || existingItem.ItemType != 1;

        if (needsUpdate)
        {
            await connection.ExecuteAsync(
                new CommandDefinition(
                    @"
UPDATE dbo.MobileTabItems
SET MobileTabGroupID = @GroupId,
    URL = @Url,
    Text = @Text,
    ItemType = 1
WHERE MobileTabItemsID = @Id;",
                    new { GroupId = groupId, Url = url, Text = text, Id = existingItem.MobileTabItemsID },
                    transaction: transaction,
                    cancellationToken: cancellationToken)).ConfigureAwait(false);
        }

        if (existingItems.Count > 1)
        {
            await DeleteMobileTabItemsAsync(connection, transaction, existingItems.Skip(1), cancellationToken).ConfigureAwait(false);
        }

        return needsUpdate || existingItems.Count > 1 ? "Updated" : "Unchanged";
    }

    private static async Task DeleteMobileTabItemsAsync(
        SqlConnection connection,
        DbTransaction transaction,
        IEnumerable<MobileTabItemRow> items,
        CancellationToken cancellationToken)
    {
        await connection.ExecuteAsync(
            new CommandDefinition(
                "DELETE FROM dbo.MobileTabItems WHERE MobileTabItemsID IN @Ids;",
                new { Ids = items.Select(static item => item.MobileTabItemsID).ToArray() },
                transaction: transaction,
                cancellationToken: cancellationToken)).ConfigureAwait(false);
    }

    private static string? TryGetStormIdentifierFromUrl(string url)
    {
        const string marker = "/FLOATER/";
        var markerIndex = url.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
        if (markerIndex < 0)
        {
            return null;
        }

        var identifierStart = markerIndex + marker.Length;
        var identifierEnd = url.IndexOf('/', identifierStart);
        if (identifierEnd < 0)
        {
            return null;
        }

        var identifier = url[identifierStart..identifierEnd].ToUpperInvariant();
        if (identifier.Length != 8
            || identifier[0..2] is not ("AL" or "EP" or "CP")
            || !identifier[2..].All(char.IsDigit))
        {
            return null;
        }

        return identifier;
    }

    private static async Task<StormRow> InsertStormAsync(
        DbConnection connection,
        DbTransaction transaction,
        string name,
        int year,
        int regionType,
        int stormType,
        bool active,
        int stormNumber,
        CancellationToken cancellationToken)
    {
        var insertedStorms = (await connection.QueryAsync<StormRow>(
            new CommandDefinition(
                "dbo.msp_Storm_Ins",
                new
                {
                    Name = name,
                    Year = year,
                    RegionType = regionType,
                    StormType = stormType,
                    Active = active,
                    StormNumber = stormNumber,
                },
                transaction: transaction,
                commandType: CommandType.StoredProcedure,
                cancellationToken: cancellationToken)).ConfigureAwait(false)).ToList();

        return insertedStorms.LastOrDefault(row =>
                row.Year == year
                && row.RegionType == regionType
                && row.StormType == stormType
                && row.StormNumber == stormNumber
                && string.Equals(row.Name, name, StringComparison.OrdinalIgnoreCase))
            ?? throw new InvalidOperationException($"Could not reload inserted storm '{name}' for year {year}, region {regionType}, storm number {stormNumber}, storm type {stormType}.");
    }

    private sealed record StormRow
    {
        public int StormID { get; init; }

        public string Name { get; init; } = string.Empty;

        public int Year { get; init; }

        public int RegionType { get; init; }

        public bool Active { get; init; }

        public int StormNumber { get; init; }

        public bool EmailAlertsSent { get; init; }

        public int StormType { get; init; }
    }

    private sealed record MobileTabItemRow
    {
        public int MobileTabItemsID { get; init; }

        public int MobileTabGroupID { get; init; }

        public string URL { get; init; } = string.Empty;

        public string Text { get; init; } = string.Empty;

        public int ItemType { get; init; }
    }

    private sealed record ActiveStormIdentityRow
    {
        public int StormNumber { get; init; }

        public int Year { get; init; }

        public int RegionType { get; init; }
    }

    private sealed record CoordinateRow
    {
        public string AdvisoryNumber { get; init; } = string.Empty;

        public string Latitude { get; init; } = string.Empty;

        public string Longitude { get; init; } = string.Empty;

        public int WindSpeed { get; init; }

        public int Pressure { get; init; }

        public int SpeedOfTravel { get; init; }

        public int Direction { get; init; }

        public DateTime CoordinateDate { get; init; }
    }
}
