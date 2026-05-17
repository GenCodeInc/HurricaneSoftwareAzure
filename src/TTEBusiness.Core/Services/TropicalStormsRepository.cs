using System.Data;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;
using TTENET.TTEBusiness.Core.Models;
using TTENET.TTEBusiness.Core.Utilities;

namespace TTENET.TTEBusiness.Core.Services;

public sealed class TropicalStormsRepository(IOptions<TteDataOptions> options) : ITropicalStormsRepository
{
    private const string DefaultMobileItemThumbnailUrl = "http://www.nhc.noaa.gov/gifs/noaaleft.jpg";

    public async Task<IReadOnlyList<GisDataItem>> GetGisDataAsync(bool activeOnly, CancellationToken cancellationToken)
    {
        await using var connection = await OpenConnectionAsync(cancellationToken).ConfigureAwait(false);

        var rows = await connection.QueryAsync<GisDataRow>(
            new CommandDefinition(
                $"SELECT Title, Description, URL, Active FROM dbo.GISData {(activeOnly ? "WHERE Active = 1" : string.Empty)} ORDER BY Sort, GISID",
                cancellationToken: cancellationToken)).ConfigureAwait(false);

        return rows.Select(static row => new GisDataItem
        {
            Title = row.Title ?? string.Empty,
            Description = row.Description ?? string.Empty,
            Url = row.URL ?? string.Empty,
            Active = row.Active,
        }).ToArray();
    }

    public async Task<IReadOnlyList<PointOfInterestItem>> GetPointsOfInterestAsync(CancellationToken cancellationToken)
    {
        await using var connection = await OpenConnectionAsync(cancellationToken).ConfigureAwait(false);

        var rows = await connection.QueryAsync<PointOfInterestRow>(
            new CommandDefinition(
                "dbo.msp_PointsOfInterest_Get",
                commandType: CommandType.StoredProcedure,
                cancellationToken: cancellationToken)).ConfigureAwait(false);

        return rows.Select(static row => new PointOfInterestItem
        {
            Id = row.ID,
            Latitude = Convert.ToSingle(row.Latitude),
            Longitude = Convert.ToSingle(row.Longitude),
            Type = row.Type,
            RelatedText = row.RelatedText ?? string.Empty,
        }).ToArray();
    }

    public async Task<IReadOnlyList<MobileTabGroupItem>> GetMobileTabGroupsAsync(int? regionType, int tabToShowOn, CancellationToken cancellationToken)
    {
        await using var connection = await OpenConnectionAsync(cancellationToken).ConfigureAwait(false);

        var itemRows = await connection.QueryAsync<MobileTabItemRow>(
            new CommandDefinition(
                "SELECT MobileTabItemsID, MobileTabGroupID, URL, Text, ItemType FROM dbo.MobileTabItems WHERE ItemType IS NOT NULL",
                cancellationToken: cancellationToken)).ConfigureAwait(false);

        var sql = "SELECT MobileTabGroupID, Header, SubHeader, ThumbnailURL, Region, TabToShowOn, ApplicationType FROM dbo.MobileTabGroup";
        var filters = new List<string>();
        var parameters = new DynamicParameters();

        if (regionType.HasValue)
        {
            filters.Add("Region = @Region");
            parameters.Add("Region", regionType.Value);
        }

        if (tabToShowOn != 0)
        {
            filters.Add("TabToShowOn = @TabToShowOn");
            parameters.Add("TabToShowOn", tabToShowOn);
        }

        if (filters.Count > 0)
        {
            sql += " WHERE " + string.Join(" AND ", filters);
        }

        var groupRows = await connection.QueryAsync<MobileTabGroupRow>(
            new CommandDefinition(sql, parameters, cancellationToken: cancellationToken)).ConfigureAwait(false);

        var items = itemRows.Select(static row => new MobileTabItem
        {
            ID = row.MobileTabItemsID,
            MobileTabGroupID = row.MobileTabGroupID,
            URL = row.URL ?? string.Empty,
            Text = row.Text ?? string.Empty,
            ThumbnailURL = row.ItemType == 1 ? row.URL ?? string.Empty : DefaultMobileItemThumbnailUrl,
            ItemType = row.ItemType,
        }).ToArray();

        return groupRows.Select(row => new MobileTabGroupItem
        {
            ID = row.MobileTabGroupID,
            Header = row.Header ?? string.Empty,
            SubHeader = row.SubHeader ?? string.Empty,
            ThumbnailURL = row.ThumbnailURL ?? string.Empty,
            GroupRegion = row.Region,
            TabToShowOn = row.TabToShowOn,
            ApplicationType = row.ApplicationType,
            MobileTabItems = items.Where(item => item.MobileTabGroupID == row.MobileTabGroupID).ToArray(),
        }).ToArray();
    }

    public async Task<IReadOnlyList<SatelliteGroupItem>> GetSatelliteGroupsAsync(int? regionType, CancellationToken cancellationToken)
    {
        await using var connection = await OpenConnectionAsync(cancellationToken).ConfigureAwait(false);

        var itemRows = await connection.QueryAsync<SatelliteItemRow>(
            new CommandDefinition(
                "SELECT SatelliteID, SatelliteGroupID, URL, Text FROM dbo.SatelliteItems",
                cancellationToken: cancellationToken)).ConfigureAwait(false);

        var sql = "SELECT SatelliteGroupID, Header, SubHeader, ThumbnailURL, Region FROM dbo.SatelliteGroup";
        var parameters = new DynamicParameters();
        if (regionType.HasValue)
        {
            sql += " WHERE Region = @Region";
            parameters.Add("Region", regionType.Value);
        }

        sql += " ORDER BY SatelliteGroupID";

        var groupRows = await connection.QueryAsync<SatelliteGroupRow>(
            new CommandDefinition(sql, parameters, cancellationToken: cancellationToken)).ConfigureAwait(false);

        var items = itemRows.Select(static row => new SatelliteItem
        {
            ID = row.SatelliteID,
            SatelliteGroupID = row.SatelliteGroupID,
            URL = row.URL ?? string.Empty,
            Text = row.Text ?? string.Empty,
            ThumbnailURL = row.URL ?? string.Empty,
        }).ToArray();

        return groupRows.Select(row => new SatelliteGroupItem
        {
            ID = row.SatelliteGroupID,
            Header = row.Header ?? string.Empty,
            SubHeader = row.SubHeader ?? string.Empty,
            ThumbnailURL = row.ThumbnailURL ?? string.Empty,
            GroupRegion = row.Region,
            SatelliteItems = items.Where(item => item.SatelliteGroupID == row.SatelliteGroupID).ToArray(),
        }).ToArray();
    }

    public async Task<IReadOnlyList<StormSummaryItem>> GetStormNamesAsync(string region, bool activeOnly, CancellationToken cancellationToken)
    {
        await using var connection = await OpenConnectionAsync(cancellationToken).ConfigureAwait(false);

        var rows = await connection.QueryAsync<StormRow>(
            new CommandDefinition(
                "dbo.msp_Storm_Get",
                new
                {
                    StormID = (int?)null,
                    Name = (string?)null,
                    RegionType = ParseRegionType(region),
                    ShowOnTTE = true,
                    EmailAlertsSent = (bool?)null,
                    Active = activeOnly ? (bool?)true : null,
                    StormNumber = (int?)null,
                    Year = (int?)null,
                },
                commandType: CommandType.StoredProcedure,
                cancellationToken: cancellationToken)).ConfigureAwait(false);

        return rows.Select(MapStormSummary).ToArray();
    }

    public async Task<StormDetailItem?> GetStormAsync(int stormId, bool withImageLinks, CancellationToken cancellationToken)
    {
        await using var connection = await OpenConnectionAsync(cancellationToken).ConfigureAwait(false);

        var row = await connection.QuerySingleOrDefaultAsync<StormRow>(
            new CommandDefinition(
                "dbo.msp_Storm_Get",
                new
                {
                    StormID = stormId,
                    Name = (string?)null,
                    RegionType = (int?)null,
                    ShowOnTTE = (bool?)null,
                    EmailAlertsSent = (bool?)null,
                    Active = (bool?)null,
                    StormNumber = (int?)null,
                    Year = (int?)null,
                },
                commandType: CommandType.StoredProcedure,
                cancellationToken: cancellationToken)).ConfigureAwait(false);

        if (row is null)
        {
            return null;
        }

        var storm = new StormDetailItem
        {
            Id = row.StormID,
            StormId = row.StormID,
            Name = row.Name ?? string.Empty,
            NameYear = $"{row.Name} {row.Year}",
            Region = FormatRegion(row.RegionType),
            Year = row.Year,
            Active = row.Active,
            StormType = row.StormType,
            EmailAlertsSent = row.EmailAlertsSent,
            StormNumber = row.StormNumber ?? 0,
            ImageUrl = $"http://www.hurricanesoftware.com/TTE/GadgetImages/GadgetImage.aspx?GadgetImage={row.Name}.jpg",
        };

        if (withImageLinks)
        {
            storm.ImageLinks = await GetImageLinksAsync(stormId, 0, cancellationToken).ConfigureAwait(false);
        }

        return storm;
    }

    public async Task<IReadOnlyList<StormDetailItem>> GetStormsAsync(
        string stormsToDownload,
        string region,
        bool withImageLinks,
        bool activeOnly,
        bool lastCoordinateOnly,
        bool forecastsToo,
        CancellationToken cancellationToken)
    {
        await using var connection = await OpenConnectionAsync(cancellationToken).ConfigureAwait(false);

        var regionType = ParseRegionType(region);
        var requestedStorms = ParseStormSelection(stormsToDownload);

        var stormRows = await connection.QueryAsync<StormRow>(
            new CommandDefinition(
                "dbo.msp_Storm_Get",
                new
                {
                    StormID = (int?)null,
                    Name = (string?)null,
                    RegionType = regionType,
                    ShowOnTTE = true,
                    EmailAlertsSent = (bool?)null,
                    Active = activeOnly ? (bool?)true : null,
                    StormNumber = (int?)null,
                    Year = (int?)null,
                },
                commandType: CommandType.StoredProcedure,
                cancellationToken: cancellationToken)).ConfigureAwait(false);

        var storms = new List<StormDetailItem>();
        foreach (var stormRow in stormRows)
        {
            var stormName = stormRow.Name ?? string.Empty;
            if (!forecastsToo && stormName.Contains("FORECAST", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (requestedStorms is not null && !requestedStorms.Contains(stormName, StringComparer.OrdinalIgnoreCase))
            {
                continue;
            }

            var coordinateRows = (await connection.QueryAsync<CoordinateRow>(
                new CommandDefinition(
                    "dbo.msp_Coordinate_Get",
                    new
                    {
                        StormID = stormRow.StormID,
                        RegionType = regionType,
                        AdvisoryNumber = (string?)null,
                    },
                    commandType: CommandType.StoredProcedure,
                    cancellationToken: cancellationToken)).ConfigureAwait(false)).ToList();

            var coordinates = coordinateRows.Select(MapCoordinate).ToList();
            var storm = new StormDetailItem
            {
                Id = stormRow.StormID,
                StormId = stormRow.StormID,
                Name = stormName,
                NameYear = $"{stormName} {stormRow.Year}",
                Region = FormatRegion(stormRow.RegionType),
                Year = stormRow.Year,
                Active = stormRow.Active,
                StormType = stormRow.StormType,
                EmailAlertsSent = stormRow.EmailAlertsSent,
                StormNumber = stormRow.StormNumber ?? 0,
                Coordinates = coordinates,
                ImageUrl = $"http://www.hurricanesoftware.com/TTE/GadgetImages/GadgetImage.aspx?GadgetImage={stormName}.jpg",
            };

            if (withImageLinks)
            {
                storm.ImageLinks = await GetImageLinksAsync(stormRow.StormID, 0, cancellationToken).ConfigureAwait(false);
            }

            ApplyStormDetails(storm, coordinates, lastCoordinateOnly);
            storms.Add(storm);
        }

        return storms;
    }

    public async Task<IReadOnlyList<CoordinateItem>> GetCoordinatesAsync(int stormId, CancellationToken cancellationToken)
    {
        await using var connection = await OpenConnectionAsync(cancellationToken).ConfigureAwait(false);

        var rows = await connection.QueryAsync<CoordinateRow>(
            new CommandDefinition(
                "dbo.msp_Coordinate_Get",
                new
                {
                    StormID = stormId,
                    RegionType = (int?)null,
                    AdvisoryNumber = (string?)null,
                },
                commandType: CommandType.StoredProcedure,
                cancellationToken: cancellationToken)).ConfigureAwait(false);

        return rows.Select(MapCoordinate).ToArray();
    }

    public async Task<IReadOnlyList<ImageLinkItem>> GetImageLinksAsync(int? stormId, int imageLinkType, CancellationToken cancellationToken)
    {
        await using var connection = await OpenConnectionAsync(cancellationToken).ConfigureAwait(false);

        var whereClauses = new List<string>();
        var parameters = new DynamicParameters();

        if (stormId.HasValue && stormId.Value != 0)
        {
            whereClauses.Add("StormID = @StormID");
            parameters.Add("StormID", stormId.Value);
        }

        if (imageLinkType != 0)
        {
            whereClauses.Add("ImageLinkType = @ImageLinkType");
            parameters.Add("ImageLinkType", imageLinkType);
        }

        var sql = "SELECT ImageLinkID, ImageLinkType, StormID, URL, DateUpated FROM dbo.ImageLink";
        if (whereClauses.Count > 0)
        {
            sql += " WHERE " + string.Join(" AND ", whereClauses);
        }

        var rows = await connection.QueryAsync<ImageLinkRow>(
            new CommandDefinition(sql, parameters, cancellationToken: cancellationToken)).ConfigureAwait(false);

        return rows.Select(static row => new ImageLinkItem
        {
            ImageLinkId = row.ImageLinkID,
            ImageLinkType = row.ImageLinkType,
            StormId = row.StormID,
            Url = row.URL ?? string.Empty,
            DateUpdated = row.DateUpated,
        }).ToArray();
    }

    public async Task<RegistrationRecordItem?> GetRegistrationAsync(string userId, CancellationToken cancellationToken)
    {
        await using var connection = await OpenConnectionAsync(cancellationToken).ConfigureAwait(false);

        var row = await connection.QuerySingleOrDefaultAsync<RegistrationRow>(
            new CommandDefinition(
                "dbo.msp_Registration_Get",
                new { UserID = userId },
                commandType: CommandType.StoredProcedure,
                cancellationToken: cancellationToken)).ConfigureAwait(false);

        return row is null ? null : MapRegistration(row);
    }

    public async Task<RegistrationRecordItem?> GetRegistrationByEmailAsync(string lookup, CancellationToken cancellationToken)
    {
        await using var connection = await OpenConnectionAsync(cancellationToken).ConfigureAwait(false);

        RegistrationRow? row;
        try
        {
            row = await connection.QuerySingleOrDefaultAsync<RegistrationRow>(
                new CommandDefinition(
                    "dbo.msp_Registration_Get_FromEmail",
                    new { LookupParm = lookup },
                    commandType: CommandType.StoredProcedure,
                    cancellationToken: cancellationToken)).ConfigureAwait(false);
        }
        catch (SqlException)
        {
            row = await connection.QuerySingleOrDefaultAsync<RegistrationRow>(
                new CommandDefinition(
                    "SELECT TOP (1) ID, UserName, RegistrationNumber, Email, QtyOrdered, ReferredBy, DateRegistered, UserID, DateExpire, CellPhoneAlert, EmailAlert FROM dbo.Registrations WHERE Email = @Lookup ORDER BY ID DESC",
                    new { Lookup = lookup },
                    cancellationToken: cancellationToken)).ConfigureAwait(false);
        }
        catch (InvalidOperationException)
        {
            row = await connection.QueryFirstOrDefaultAsync<RegistrationRow>(
                new CommandDefinition(
                    "SELECT ID, UserName, RegistrationNumber, Email, QtyOrdered, ReferredBy, DateRegistered, UserID, DateExpire, CellPhoneAlert, EmailAlert FROM dbo.Registrations WHERE Email = @Lookup ORDER BY ID DESC",
                    new { Lookup = lookup },
                    cancellationToken: cancellationToken)).ConfigureAwait(false);
        }

        return row is null ? null : MapRegistration(row);
    }

    public async Task<IReadOnlyList<RegistrationRecordItem>> GetRegistrationsByEmailAlertAsync(string value, CancellationToken cancellationToken)
    {
        await using var connection = await OpenConnectionAsync(cancellationToken).ConfigureAwait(false);

        var rows = await connection.QueryAsync<RegistrationRow>(
            new CommandDefinition(
                "SELECT ID, UserName, RegistrationNumber, Email, QtyOrdered, ReferredBy, DateRegistered, UserID, DateExpire, CellPhoneAlert, EmailAlert FROM dbo.Registrations WHERE EmailAlert = @Value",
                new { Value = value },
                cancellationToken: cancellationToken)).ConfigureAwait(false);

        return rows.Select(MapRegistration).ToArray();
    }

    public async Task<IReadOnlyList<RegistrationRecordItem>> GetRegistrationsByCellAlertAsync(string value, CancellationToken cancellationToken)
    {
        await using var connection = await OpenConnectionAsync(cancellationToken).ConfigureAwait(false);

        var rows = await connection.QueryAsync<RegistrationRow>(
            new CommandDefinition(
                "SELECT ID, UserName, RegistrationNumber, Email, QtyOrdered, ReferredBy, DateRegistered, UserID, DateExpire, CellPhoneAlert, EmailAlert FROM dbo.Registrations WHERE CellPhoneAlert = @Value",
                new { Value = value },
                cancellationToken: cancellationToken)).ConfigureAwait(false);

        return rows.Select(MapRegistration).ToArray();
    }

    public async Task CreateRegistrationAsync(RegistrationRecordItem registration, CancellationToken cancellationToken)
    {
        await using var connection = await OpenConnectionAsync(cancellationToken).ConfigureAwait(false);

        await connection.ExecuteAsync(
            new CommandDefinition(
                "INSERT INTO dbo.Registrations (UserName, RegistrationNumber, Email, QtyOrdered, ReferredBy, DateRegistered, UserID, DateExpire, CellPhoneAlert, EmailAlert) VALUES (@UserName, @RegistrationNumber, @Email, @QtyOrdered, @ReferredBy, @DateRegistered, @UserID, @DateExpire, @CellPhoneAlert, @EmailAlert)",
                new
                {
                    registration.UserName,
                    registration.RegistrationNumber,
                    registration.Email,
                    registration.QtyOrdered,
                    registration.ReferredBy,
                    registration.DateRegistered,
                    UserID = registration.UserId,
                    registration.DateExpire,
                    registration.CellPhoneAlert,
                    registration.EmailAlert,
                },
                cancellationToken: cancellationToken)).ConfigureAwait(false);
    }

    public async Task UpdateRegistrationAsync(RegistrationRecordItem registration, CancellationToken cancellationToken)
    {
        await using var connection = await OpenConnectionAsync(cancellationToken).ConfigureAwait(false);

        await connection.ExecuteAsync(
            new CommandDefinition(
                "UPDATE dbo.Registrations SET UserName = @UserName, RegistrationNumber = @RegistrationNumber, Email = @Email, QtyOrdered = @QtyOrdered, ReferredBy = @ReferredBy, DateRegistered = @DateRegistered, UserID = @UserID, DateExpire = @DateExpire, CellPhoneAlert = @CellPhoneAlert, EmailAlert = @EmailAlert WHERE ID = @Id",
                new
                {
                    registration.Id,
                    registration.UserName,
                    registration.RegistrationNumber,
                    registration.Email,
                    registration.QtyOrdered,
                    registration.ReferredBy,
                    registration.DateRegistered,
                    UserID = registration.UserId,
                    registration.DateExpire,
                    registration.CellPhoneAlert,
                    registration.EmailAlert,
                },
                cancellationToken: cancellationToken)).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<AlertRecordItem>> GetAlertsAsync(
        int? alertId,
        int? alertTypeId,
        string? value,
        bool? confirmed,
        int? applicationTypeId,
        string? externalKey,
        CancellationToken cancellationToken)
    {
        await using var connection = await OpenConnectionAsync(cancellationToken).ConfigureAwait(false);

        var rows = await connection.QueryAsync<AlertRow>(
            new CommandDefinition(
                "dbo.msp_Alert_Get",
                new
                {
                    AlertID = alertId,
                    AlertTypeID = alertTypeId,
                    Value = value,
                    Confirmed = confirmed,
                    AppTypeID = applicationTypeId,
                    ExternalKey = externalKey,
                },
                commandType: CommandType.StoredProcedure,
                cancellationToken: cancellationToken)).ConfigureAwait(false);

        return rows.Select(static row => new AlertRecordItem
        {
            Id = row.AlertID,
            AlertTypeId = row.AlertTypeID,
            ApplicationTypeId = row.AppTypeID,
            Value = row.Value ?? string.Empty,
            Confirmed = row.Confirmed,
            ExternalKey = row.ExternalKey ?? string.Empty,
        }).ToArray();
    }

    public async Task<AlertRecordItem> CreateAlertAsync(int alertTypeId, string value, bool confirmed, int applicationTypeId, string externalKey, CancellationToken cancellationToken)
    {
        await using var connection = await OpenConnectionAsync(cancellationToken).ConfigureAwait(false);

        await connection.ExecuteAsync(
            new CommandDefinition(
                "dbo.msp_Alert_Ins",
                new
                {
                    AlertTypeID = alertTypeId,
                    Value = value,
                    Confirmed = confirmed,
                    AppTypeID = applicationTypeId,
                    ExternalKey = externalKey,
                },
                commandType: CommandType.StoredProcedure,
                cancellationToken: cancellationToken)).ConfigureAwait(false);

        var created = (await GetAlertsAsync(null, alertTypeId, value, confirmed, applicationTypeId, externalKey, cancellationToken).ConfigureAwait(false))
            .OrderBy(alert => alert.Id)
            .LastOrDefault();

        if (created is null)
        {
            throw new InvalidOperationException("Alert creation succeeded but the inserted alert could not be reloaded.");
        }

        return created;
    }

    public async Task UpdateAlertConfirmationAsync(int alertId, bool confirmed, CancellationToken cancellationToken)
    {
        await using var connection = await OpenConnectionAsync(cancellationToken).ConfigureAwait(false);

        await connection.ExecuteAsync(
            new CommandDefinition(
                "UPDATE dbo.Alerts SET Confirmed = @Confirmed WHERE AlertID = @AlertID",
                new { AlertID = alertId, Confirmed = confirmed },
                cancellationToken: cancellationToken)).ConfigureAwait(false);
    }

    public async Task DeleteAlertAsync(int alertId, CancellationToken cancellationToken)
    {
        await using var connection = await OpenConnectionAsync(cancellationToken).ConfigureAwait(false);

        await connection.ExecuteAsync(
            new CommandDefinition(
                "dbo.msp_Alert_Del",
                new { AlertID = alertId },
                commandType: CommandType.StoredProcedure,
                cancellationToken: cancellationToken)).ConfigureAwait(false);
    }

    public async Task<long> IncrementApplicationHitCounterAsync(int applicationTypeId, CancellationToken cancellationToken)
    {
        await using var connection = await OpenConnectionAsync(cancellationToken).ConfigureAwait(false);

        var logDate = DateTime.Today;
        var row = await connection.QuerySingleOrDefaultAsync<ApplicationHitCounterRow>(
            new CommandDefinition(
                "dbo.msp_ApplicationHitCounter_Get",
                new { ApplicationTypeID = applicationTypeId, LogDate = logDate },
                commandType: CommandType.StoredProcedure,
                cancellationToken: cancellationToken)).ConfigureAwait(false);

        if (row is null)
        {
            await connection.ExecuteAsync(
                new CommandDefinition(
                    "dbo.msp_ApplicationHitCounter_Ins",
                    new { ApplicationTypeID = applicationTypeId, LogDate = logDate },
                    commandType: CommandType.StoredProcedure,
                    cancellationToken: cancellationToken)).ConfigureAwait(false);

            return 1;
        }

        var newCount = row.Counter + 1;
        await connection.ExecuteAsync(
            new CommandDefinition(
                "dbo.msp_ApplicationHitCounter_Upd",
                new { NewCount = newCount, ApplicationTypeID = applicationTypeId, LogDate = logDate },
                commandType: CommandType.StoredProcedure,
                cancellationToken: cancellationToken)).ConfigureAwait(false);

        return newCount;
    }

    public async Task<bool> HasInvalidUserAsync(string userId, CancellationToken cancellationToken)
    {
        await using var connection = await OpenConnectionAsync(cancellationToken).ConfigureAwait(false);

        var rows = await connection.QueryAsync<InvalidUserRow>(
            new CommandDefinition(
                "dbo.msp_InvalidUser_Get",
                new { UserID = userId },
                commandType: CommandType.StoredProcedure,
                cancellationToken: cancellationToken)).ConfigureAwait(false);

        return rows.Any();
    }

    public async Task<IReadOnlyList<AppLinkItem>> GetAppLinksAsync(
        string userId,
        string registrationNumber,
        string osBinaryTime,
        int numberOfTimesLoggedIn,
        string promo,
        int? appLinkType,
        int? regionType,
        bool? active,
        CancellationToken cancellationToken)
    {
        await using var connection = await OpenConnectionAsync(cancellationToken).ConfigureAwait(false);

        var rows = await connection.QueryAsync<AppLinkRow>(
            new CommandDefinition(
                "dbo.msp_AppLink_Get",
                new
                {
                    AppLinkType = appLinkType,
                    RegionType = regionType,
                    Active = active,
                },
                commandType: CommandType.StoredProcedure,
                cancellationToken: cancellationToken)).ConfigureAwait(false);

        return rows.Select(row => new AppLinkItem
        {
            Id = row.ID,
            RegionType = row.RegionType,
            AppLinkType = row.AppLinkType,
            Active = row.Active,
            Url = BuildAppLinkUrl(row.URL ?? string.Empty, row.AppLinkType, userId, registrationNumber, osBinaryTime, numberOfTimesLoggedIn, promo),
        }).ToArray();
    }

    public async Task<VersionInfoResult> GetVersionInfoAsync(int applicationType, string version, string promo, bool getZip, CancellationToken cancellationToken)
    {
        await using var connection = await OpenConnectionAsync(cancellationToken).ConfigureAwait(false);

        var rows = (await connection.QueryAsync<VersionCheckRow>(
            new CommandDefinition(
                "dbo.msp_VersionCheck_Get",
                new
                {
                    ApplicationType = applicationType,
                    Version = string.IsNullOrWhiteSpace(version) ? null : version,
                },
                commandType: CommandType.StoredProcedure,
                cancellationToken: cancellationToken)).ConfigureAwait(false)).ToList();

        if (rows.Count == 0)
        {
            rows = (await connection.QueryAsync<VersionCheckRow>(
                new CommandDefinition(
                    "dbo.msp_VersionCheck_Get",
                    new
                    {
                        ApplicationType = applicationType,
                        Version = (string?)null,
                    },
                    commandType: CommandType.StoredProcedure,
                    cancellationToken: cancellationToken)).ConfigureAwait(false)).ToList();
        }

        var row = rows.FirstOrDefault();
        if (row is null)
        {
            return new VersionInfoResult
            {
                ReturnMessage = new ReturnMessage(2, "Version information is not available."),
            };
        }

        var suffix = getZip ? "setuptte.zip" : "setuptte.exe";
        var promoSegment = string.IsNullOrWhiteSpace(promo) ? "ttelatest" : promo.Trim();
        var downloadLocation = $"{row.DownLoadLocationRoot?.TrimEnd('/')}/{promoSegment}/{suffix}";
        var runningLatest = string.Equals(row.LatestVersion, version, StringComparison.OrdinalIgnoreCase);

        return new VersionInfoResult
        {
            LatestVersion = row.LatestVersion ?? string.Empty,
            SharewareLimit = row.SharewareLimit,
            DownloadLocation = downloadLocation,
            RunningLatestVersion = runningLatest,
            RequiredUpdate = false,
            ReturnMessage = runningLatest
                ? new ReturnMessage(0, "You are running the latest version.")
                : new ReturnMessage(1, "There is a newer version of Tracking The Eye avaliable."),
        };
    }

    private async Task<SqlConnection> OpenConnectionAsync(CancellationToken cancellationToken)
    {
        var connectionString = options.Value.SqlConnectionString;
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException("TTE data SQL connection string is not configured.");
        }

        var connection = new SqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
        return connection;
    }

    private static string BuildAppLinkUrl(string url, int appLinkType, string userId, string registrationNumber, string osBinaryTime, int numberOfTimesLoggedIn, string promo)
    {
        return appLinkType switch
        {
            12 => $"{url}?UserID={userId}&RegistrationNumber={registrationNumber}&Promo={promo}",
            9 => $"{url}?UserID={userId}&RegistrationNumber={registrationNumber}&osBinaryTime={osBinaryTime}&NumberOfTimesLoggedIn={numberOfTimesLoggedIn}&Promo={promo}",
            11 => $"{url}?UserID={userId}&RegistrationNumber={registrationNumber}&Source=TTE&Promo={promo}",
            18 => $"{url}?UserID={userId}&RegistrationNumber={registrationNumber}&Promo={promo}",
            _ => url,
        };
    }

    private static StormSummaryItem MapStormSummary(StormRow row) => new()
    {
        Id = row.StormID,
        StormId = row.StormID,
        Name = row.Name ?? string.Empty,
        NameYear = $"{row.Name} {row.Year}",
        Region = FormatRegion(row.RegionType),
        Year = row.Year,
        Active = row.Active,
        StormType = row.StormType,
        EmailAlertsSent = row.EmailAlertsSent,
        StormNumber = row.StormNumber ?? 0,
    };

    private static RegistrationRecordItem MapRegistration(RegistrationRow row) => new()
    {
        Id = row.ID,
        UserName = row.UserName ?? string.Empty,
        CellPhoneAlert = row.CellPhoneAlert ?? string.Empty,
        EmailAlert = row.EmailAlert ?? string.Empty,
        UserId = row.UserID ?? string.Empty,
        RegistrationNumber = RegistrationCodeUtility.GetRegCode(row.UserID ?? string.Empty),
        Email = row.Email ?? string.Empty,
        QtyOrdered = row.QtyOrdered,
        ReferredBy = row.ReferredBy,
        DateRegistered = row.DateRegistered,
        DateExpire = row.DateExpire,
    };

    private static CoordinateItem MapCoordinate(CoordinateRow row)
    {
        var windSpeed = row.WindSpeed;
        return new CoordinateItem
        {
            StormId = row.StormID,
            AdvisoryNumber = row.AdvisoryNumber ?? string.Empty,
            Latitude = Convert.ToSingle(row.Latitude),
            Longitude = Convert.ToSingle(row.Longitude),
            WindSpeed = windSpeed,
            SpeedTravel = row.SpeedOfTravel,
            Pressure = row.Pressure,
            Direction = row.Direction,
            Heading = ToHeading(row.Direction),
            UtcOffset = TimeZoneInfo.Local.GetUtcOffset(DateTime.Now).ToString(),
            StormType = DetermineStormType(windSpeed),
            CoordinateDate = row.CoordinateDate,
            CoordinateType = row.CoordinateType,
        };
    }

    private static HashSet<string>? ParseStormSelection(string stormsToDownload)
    {
        if (string.IsNullOrWhiteSpace(stormsToDownload) || string.Equals(stormsToDownload, "All", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        return stormsToDownload
            .Replace("FORCAST", "FORECAST", StringComparison.OrdinalIgnoreCase)
            .Split(['~', ',', ';', '|'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(static value => value.Trim())
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
    }

    private static void ApplyStormDetails(StormDetailItem storm, List<CoordinateItem> coordinates, bool lastCoordinateOnly)
    {
        if (coordinates.Count == 0)
        {
            return;
        }

        var lastCoordinate = coordinates[^1];
        var highestWindSpeed = coordinates.Max(static coordinate => coordinate.WindSpeed);
        var highestStormType = coordinates.Max(static coordinate => coordinate.StormType);

        storm.StormType = storm.StormType == 0 ? lastCoordinate.StormType : highestStormType;
        storm.Details = BuildStormDetails(storm, highestWindSpeed, lastCoordinate);

        if (lastCoordinateOnly)
        {
            storm.Coordinates = [lastCoordinate];
        }
    }

    private static string BuildStormDetails(StormDetailItem storm, int highestWindSpeed, CoordinateItem lastCoordinate)
    {
        var activeText = storm.Active ? string.Empty : "not";
        var parts = new List<string>
        {
            $"Storm name is {storm.Name}.  This storm is {activeText} an active storm.",
        };

        if (!storm.Active)
        {
            parts.Add($"This storms highest windspeed was reported to be {highestWindSpeed} mph.");
        }

        parts.Add($"Last reported wind speeds by the NHC were at {lastCoordinate.WindSpeed} mph with a heading of {lastCoordinate.Heading} and traveling at {lastCoordinate.SpeedTravel} mph.");
        parts.Add($"Storms last reported heading was {lastCoordinate.Heading} and traveling at {lastCoordinate.SpeedTravel} mph.");
        parts.Add($"The last reported location of {storm.Name} by the NHC was at Latitude: {lastCoordinate.Latitude} and Longitude: {lastCoordinate.Longitude} at {lastCoordinate.CoordinateDate}.");
        parts.Add($"{storm.Name}'s barometric pressure is {lastCoordinate.Pressure} millibars.");
        parts.Add("To see Map and Images go back to storm list and select the Storm Name not the Blue Arrow.");

        return string.Join("\n\n", parts);
    }

    private static int DetermineStormType(int windSpeed) => windSpeed switch
    {
        > 0 and <= 38 => 1,
        >= 39 and <= 73 => 3,
        >= 74 => 4,
        _ => 0,
    };

    private static int? ParseRegionType(string region)
    {
        return region switch
        {
            "All" => null,
            "Atlantic" => 1,
            "Eastern Pacific" or "EasternPacific" => 2,
            "South Indian" or "SouthIndian" => 5,
            "South West Pacific" or "SouthWestPacific" => 4,
            _ => 0,
        };
    }

    private static string FormatRegion(int regionType) => regionType switch
    {
        1 => "Atlantic",
        2 => "Eastern Pacific",
        3 => "North West Pacific",
        4 => "South West Pacific",
        5 => "South Indian",
        6 => "North Indian",
        _ => string.Empty,
    };

    private static string ToHeading(int direction) => direction switch
    {
        1 => "N",
        2 => "NNE",
        3 => "NE",
        4 => "ENE",
        5 => "E",
        6 => "ESE",
        7 => "SE",
        8 => "SSE",
        9 => "S",
        10 => "SSW",
        11 => "SW",
        12 => "WSW",
        13 => "W",
        14 => "WNW",
        15 => "NW",
        16 => "NNW",
        _ => string.Empty,
    };

    private sealed class GisDataRow
    {
        public string? Title { get; init; }

        public string? Description { get; init; }

        public string? URL { get; init; }

        public bool Active { get; init; }
    }

    private sealed class PointOfInterestRow
    {
        public int ID { get; init; }

        public decimal Latitude { get; init; }

        public decimal Longitude { get; init; }

        public int Type { get; init; }

        public string? RelatedText { get; init; }
    }

    private sealed class MobileTabGroupRow
    {
        public int MobileTabGroupID { get; init; }

        public string? Header { get; init; }

        public string? SubHeader { get; init; }

        public string? ThumbnailURL { get; init; }

        public int Region { get; init; }

        public int TabToShowOn { get; init; }

        public int ApplicationType { get; init; }
    }

    private sealed class MobileTabItemRow
    {
        public int MobileTabItemsID { get; init; }

        public int MobileTabGroupID { get; init; }

        public string? URL { get; init; }

        public string? Text { get; init; }

        public int ItemType { get; init; }
    }

    private sealed class SatelliteGroupRow
    {
        public int SatelliteGroupID { get; init; }

        public string? Header { get; init; }

        public string? SubHeader { get; init; }

        public string? ThumbnailURL { get; init; }

        public int Region { get; init; }
    }

    private sealed class SatelliteItemRow
    {
        public int SatelliteID { get; init; }

        public int SatelliteGroupID { get; init; }

        public string? URL { get; init; }

        public string? Text { get; init; }
    }

    private sealed class RegistrationRow
    {
        public int ID { get; init; }

        public string? UserName { get; init; }

        public string? RegistrationNumber { get; init; }

        public string? Email { get; init; }

        public int QtyOrdered { get; init; }

        public int ReferredBy { get; init; }

        public DateTime DateRegistered { get; init; }

        public string? UserID { get; init; }

        public DateTime DateExpire { get; init; }

        public string? CellPhoneAlert { get; init; }

        public string? EmailAlert { get; init; }
    }

    private sealed class AlertRow
    {
        public int AlertID { get; init; }

        public int AlertTypeID { get; init; }

        public int AppTypeID { get; init; }

        public string? Value { get; init; }

        public bool Confirmed { get; init; }

        public string? ExternalKey { get; init; }
    }

    private sealed class InvalidUserRow
    {
        public int ID { get; init; }

        public string? RedirectToError { get; init; }

        public string? UserID { get; init; }

        public string? osBinaryTime { get; init; }

        public string? RemoteHost { get; init; }
    }

    private sealed class ApplicationHitCounterRow
    {
        public long Counter { get; init; }
    }

    private sealed class AppLinkRow
    {
        public int ID { get; init; }

        public string? URL { get; init; }

        public int RegionType { get; init; }

        public int AppLinkType { get; init; }

        public bool Active { get; init; }
    }

    private sealed class VersionCheckRow
    {
        public string? LatestVersion { get; init; }

        public string? DownLoadLocationRoot { get; init; }

        public int SharewareLimit { get; init; }
    }

    private sealed class ImageLinkRow
    {
        public int ImageLinkID { get; init; }

        public int ImageLinkType { get; init; }

        public int StormID { get; init; }

        public string? URL { get; init; }

        public DateTime DateUpated { get; init; }
    }

    private sealed record StormRow
    {
        public int StormID { get; init; }

        public string? Name { get; init; }

        public int Year { get; init; }

        public int RegionType { get; init; }

        public int StormType { get; init; }

        public bool EmailAlertsSent { get; init; }

        public bool Active { get; init; }

        public bool ShowOnTTE { get; init; }

        public int? StormNumber { get; init; }

        public DateTime? LastCoordDate { get; init; }
    }

    private sealed class CoordinateRow
    {
        public int CoordinateID { get; init; }

        public int StormID { get; init; }

        public string? AdvisoryNumber { get; init; }

        public decimal Latitude { get; init; }

        public decimal Longitude { get; init; }

        public int WindSpeed { get; init; }

        public int Pressure { get; init; }

        public int SpeedOfTravel { get; init; }

        public int Direction { get; init; }

        public int RegionType { get; init; }

        public int CoordinateType { get; init; }

        public DateTime CoordinateDate { get; init; }
    }
}
