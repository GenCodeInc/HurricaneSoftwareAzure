using System.Data;
using System.Globalization;
using System.Net.Mail;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TTENET.TTEBusiness.Core.Models;
using TTENET.TTEBusiness.Core.Services;
using TTENET.TTEBusiness.Core.Utilities;

namespace TropicalStorms.Api.Services;

public sealed class TropicalStormsFacade(
    ITropicalStormsRepository repository,
    ITropicalStormsEmailSender emailSender,
    IOptions<WebsiteAcsEmailOptions> emailOptions,
    ILogger<TropicalStormsFacade> logger) : ITropicalStormsFacade
{
    private const int TrackingTheEyeApplicationType = 1;
    private const int GadgetApplicationType = 2;
    private const int LoginMessageType = 13;
    private const int LoginMessageRegistrationExpired = 14;
    private const int LoginMessageSharewareLimit = 15;
    private const int LoginMessageHacker = 17;
    private const double GadgetRefreshMilliseconds = 300000d;

    public async Task<IReadOnlyList<SatelliteGroupItem>> GetMobileSatelliteTabAsync(int appTypeId, int regionType, CancellationToken cancellationToken)
    {
        _ = appTypeId;
        return await repository.GetSatelliteGroupsAsync(regionType == 0 ? null : regionType, cancellationToken).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<MobileTabGroupItem>> GetMobileTabsAsync(int appTypeId, int regionType, int tabToShowOn, CancellationToken cancellationToken)
    {
        var groups = await repository.GetMobileTabGroupsAsync(regionType == 0 ? null : regionType, tabToShowOn, cancellationToken).ConfigureAwait(false);
        return groups.Where(group => appTypeId == 0 || group.ApplicationType == appTypeId).ToArray();
    }

    public Task<string> CreateAlertsAsync(int alertTypeId, string email, int appTypeId, string deviceId, int regionType, CancellationToken cancellationToken)
        => CreateAlertAsync(alertTypeId, email, appTypeId, deviceId, regionType, cancellationToken);

    public async Task<string> CreateAlertAsync(int alertTypeId, string email, int appTypeId, string deviceId, int regionType, CancellationToken cancellationToken)
    {
        _ = regionType;

        var normalizedDeviceId = string.IsNullOrWhiteSpace(deviceId) ? "NA" : deviceId.Trim();
        var normalizedEmail = email?.Trim() ?? string.Empty;
        if (!string.IsNullOrWhiteSpace(normalizedEmail) && !MailAddress.TryCreate(normalizedEmail, out _))
        {
            return "Invalid email, please enter a valid email";
        }

        var removedEmails = new List<string>();
        var alreadyConfirmed = false;
        var removedAlerts = false;

        if (!string.IsNullOrWhiteSpace(normalizedEmail))
        {
            var alertsByEmail = await repository.GetAlertsAsync(null, alertTypeId, normalizedEmail, null, null, null, cancellationToken).ConfigureAwait(false);
            foreach (var alert in alertsByEmail)
            {
                alreadyConfirmed |= alert.Confirmed;
                await repository.DeleteAlertAsync(alert.Id, cancellationToken).ConfigureAwait(false);
                removedAlerts = true;
                removedEmails.Add(alert.Value);
            }
        }

        var alertsByKey = await repository.GetAlertsAsync(null, alertTypeId, null, null, null, normalizedDeviceId, cancellationToken).ConfigureAwait(false);
        foreach (var alert in alertsByKey)
        {
            await repository.DeleteAlertAsync(alert.Id, cancellationToken).ConfigureAwait(false);
            removedAlerts = true;
            removedEmails.Add(alert.Value);
        }

        if (string.IsNullOrWhiteSpace(normalizedEmail))
        {
            return removedAlerts
                ? $"Your previous alert(s) has been successfully removed. {string.Join(' ', removedEmails.Distinct(StringComparer.OrdinalIgnoreCase)).Trim()}".Trim()
                : string.Empty;
        }

        var confirmed = alreadyConfirmed || !emailSender.IsEnabled;
        await repository.CreateAlertAsync(alertTypeId, normalizedEmail, confirmed, appTypeId, normalizedDeviceId, cancellationToken).ConfigureAwait(false);

        if (!alreadyConfirmed && emailSender.IsEnabled)
        {
            var subject = "Tracking The Eye Alerts";
            var body = $"An alert was requested for {normalizedEmail}.\n\nApplication Type: {appTypeId}\nDevice ID: {normalizedDeviceId}";
            await emailSender.SendAsync(normalizedEmail, subject, body, cancellationToken).ConfigureAwait(false);

            return $"A confirmation email was sent to {normalizedEmail} from {emailOptions.Value.SenderAddress}\nonce you confirm the email is valid alerts will be activated.\nif you do not recieve a confirmation email please come back here and verify it was entered correctly.";
        }

        return alreadyConfirmed
            ? $"The email {normalizedEmail} has already been confirmed in our system."
            : $"Alert has been created for {normalizedEmail}.";
    }

    public async Task<string> RemoveAlertAsync(string value, int regionType, CancellationToken cancellationToken)
    {
        _ = regionType;

        if (string.IsNullOrWhiteSpace(value))
        {
            return "Can't remove blank";
        }

        try
        {
            var alerts = await repository.GetAlertsAsync(null, null, value.Trim(), null, null, null, cancellationToken).ConfigureAwait(false);
            foreach (var alert in alerts)
            {
                await repository.DeleteAlertAsync(alert.Id, cancellationToken).ConfigureAwait(false);
            }

            var emailRegistrations = await repository.GetRegistrationsByEmailAlertAsync(value.Trim(), cancellationToken).ConfigureAwait(false);
            foreach (var registration in emailRegistrations)
            {
                registration.EmailAlert = string.Empty;
                await repository.UpdateRegistrationAsync(registration, cancellationToken).ConfigureAwait(false);
            }

            var cellRegistrations = await repository.GetRegistrationsByCellAlertAsync(value.Trim(), cancellationToken).ConfigureAwait(false);
            foreach (var registration in cellRegistrations)
            {
                registration.CellPhoneAlert = string.Empty;
                await repository.UpdateRegistrationAsync(registration, cancellationToken).ConfigureAwait(false);
            }

            return $"All alerts for all regions have been removed for {value.Trim()}";
        }
        catch
        {
            return "There was an error removing this alert";
        }
    }

    public string GetRegCode(string userId)
    {
        try
        {
            var regCode = RegistrationCodeUtility.GetRegCode(userId);
            var adminAddress = emailOptions.Value.AdminAddress;
            if (emailSender.IsEnabled && !string.IsNullOrWhiteSpace(adminAddress))
            {
                var body = $"userid = {userId}\nregCode = {regCode}";
                _ = emailSender.SendAsync(adminAddress, "A reg code was sent from the web service.", body, CancellationToken.None);
            }

            return "reg code has been sent and your ip has been logged for security reasons.";
        }
        catch
        {
            return "There was an error in getting your registration information.  Please check that the email address is valid.";
        }
    }

    public async Task<ReturnMessage> RetrieveRegistrationAsync(string email, CancellationToken cancellationToken)
    {
        var normalizedEmail = email?.Trim() ?? string.Empty;
        if (!MailAddress.TryCreate(normalizedEmail, out _))
        {
            return new ReturnMessage(2, "Please make sure you entered a valid email address.");
        }

        RegistrationRecordItem? registration;
        try
        {
            registration = await repository.GetRegistrationByEmailAsync(normalizedEmail, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Failed to retrieve registration for {Email}.", normalizedEmail);
            return new ReturnMessage(2, "There was an error retrieving your registration information. Please try again shortly.");
        }

        if (registration is null || string.IsNullOrWhiteSpace(registration.UserId))
        {
            return new ReturnMessage(1, $"No registration code was found for email address {normalizedEmail}");
        }

        if (!emailSender.IsEnabled)
        {
            return new ReturnMessage(0, $"Registration information was found for {normalizedEmail}, but email delivery is not configured.");
        }

        var body = $"UserID = {registration.UserId}\nRegistrationCode = {registration.RegistrationNumber}\nDateExpire = {registration.DateExpire.ToString("u", CultureInfo.InvariantCulture)}";

        try
        {
            await emailSender.SendAsync(normalizedEmail, "Tracking The Eye Registration Code", body, cancellationToken).ConfigureAwait(false);
            return new ReturnMessage(0, $"Your registration information has been sent to {normalizedEmail}");
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Failed to send registration recovery email to {Email}.", normalizedEmail);
            return new ReturnMessage(2, $"We found registration information for {normalizedEmail}, but there was a problem sending the email. Please try again shortly.");
        }
    }

    public async Task<UserResult> LoginUserAsync(string userId, string registrationNumber, string osBinaryTime, int numberOfTimesLoggedIn, bool isRegistered, string version, string promo, CancellationToken cancellationToken)
    {
        var normalizedUserId = (userId ?? string.Empty).Trim().ToUpperInvariant();
        var normalizedRegistrationNumber = (registrationNumber ?? string.Empty).Trim().ToUpperInvariant();

        await repository.IncrementApplicationHitCounterAsync(TrackingTheEyeApplicationType, cancellationToken).ConfigureAwait(false);

        var versionInfo = await repository.GetVersionInfoAsync(TrackingTheEyeApplicationType, version, promo, false, cancellationToken).ConfigureAwait(false);
        var user = new UserResult
        {
            VersionInfo = versionInfo,
            LoginMessageType = LoginMessageType,
            ShowLoginMessage = false,
            LoggedIn = true,
            NeedToRegister = false,
            RunningLatestVersion = versionInfo.RunningLatestVersion,
            SharewareLimit = versionInfo.SharewareLimit,
        };

        var registration = default(RegistrationRecordItem);
        var registrationCount = 0;
        var daysUntilExpiration = 0;

        isRegistered = RegistrationCodeUtility.Validate(normalizedUserId, normalizedRegistrationNumber);
        if (isRegistered)
        {
            registration = await repository.GetRegistrationAsync(normalizedUserId, cancellationToken).ConfigureAwait(false);
            if (registration is not null)
            {
                registrationCount = 1;
                daysUntilExpiration = (registration.DateExpire - DateTime.Now).Days;
            }
            else
            {
                isRegistered = false;
            }
        }

        if (isRegistered && daysUntilExpiration < -1)
        {
            user.LoginMessageType = LoginMessageRegistrationExpired;
            user.ShowLoginMessage = true;
            user.NeedToRegister = true;
            user.LoggedIn = false;
        }
        else if (!isRegistered && numberOfTimesLoggedIn > user.SharewareLimit)
        {
            user.LoginMessageType = LoginMessageSharewareLimit;
            user.ShowLoginMessage = true;
            user.NeedToRegister = true;
            user.LoggedIn = false;
        }
        else if (registrationCount > 1)
        {
        }
        else if (numberOfTimesLoggedIn < 0)
        {
            user.LoginMessageType = LoginMessageHacker;
            user.ShowLoginMessage = true;
            user.NeedToRegister = true;
            user.LoggedIn = false;
        }

        if (!string.IsNullOrWhiteSpace(normalizedUserId) && await repository.HasInvalidUserAsync(normalizedUserId, cancellationToken).ConfigureAwait(false))
        {
            user.LoginMessageType = LoginMessageHacker;
            user.ShowLoginMessage = true;
            user.NeedToRegister = true;
            user.LoggedIn = false;
        }

        user.AppLinks = await repository.GetAppLinksAsync(normalizedUserId, normalizedRegistrationNumber, osBinaryTime, numberOfTimesLoggedIn, promo, null, null, true, cancellationToken).ConfigureAwait(false);
        user.ReturnMessage = user.LoggedIn
            ? new ReturnMessage(0, "Login successful.")
            : new ReturnMessage(1, "User login requires registration or update.");

        return user;
    }

    public async Task<ReturnMessage> ValidateUserAsync(string userId, string registrationNumber, CancellationToken cancellationToken)
    {
        var normalizedUserId = (userId ?? string.Empty).Trim().ToUpperInvariant();
        var normalizedRegistrationNumber = (registrationNumber ?? string.Empty).Trim().ToUpperInvariant();

        if (!RegistrationCodeUtility.Validate(normalizedUserId, normalizedRegistrationNumber))
        {
            return new ReturnMessage(2, "The UserID and Registration Number is invalid. Check the that information is correct, be sure you are not using any spaces and re-enter the correct registration and press update subscription again.");
        }

        var registration = await repository.GetRegistrationAsync(normalizedUserId, cancellationToken).ConfigureAwait(false);
        if (registration is null)
        {
            return new ReturnMessage(2, "The UserID and Registration Number is invalid. Check the that information is correct, be sure you are not using any spaces and re-enter the correct registration and press update subscription again.");
        }

        return new ReturnMessage(0, $"Congratulations {registration.UserName}, your registration code has been updated.");
    }

    public async Task<GadgetResult> GetGadgetAsync(string region, int gadgetType, string version, CancellationToken cancellationToken)
    {
        await repository.IncrementApplicationHitCounterAsync(GadgetApplicationType, cancellationToken).ConfigureAwait(false);

        var result = new GadgetResult
        {
            Timer = GadgetRefreshMilliseconds,
            VersionInfo = new VersionInfoResult
            {
                RunningLatestVersion = true,
                RequiredUpdate = true,
                DownloadLocation = "http://gadget.hurricanesoftware.com?DownloadLatest=true&Promo=Gadget&Gadget=true",
                ReturnMessage = new ReturnMessage(0, "You are running the latest version."),
            },
        };

        var activeOnly = gadgetType != 2;
        var forecastsToo = gadgetType == 2;
        result.Storms = await repository.GetStormsAsync("All", region, false, activeOnly, true, forecastsToo, cancellationToken).ConfigureAwait(false);

        _ = version;
        return result;
    }

    public Task<IReadOnlyList<StormDetailItem>> GetStormsAsync(string stormsToDownload, string region, bool withImageLinks, bool activeOnly, bool lastCoordinateOnly, bool omitForecastsToo, CancellationToken cancellationToken)
        => repository.GetStormsAsync(stormsToDownload, region, withImageLinks, activeOnly, lastCoordinateOnly, !omitForecastsToo, cancellationToken);

    public async Task<IReadOnlyList<LegacyStormNameItem>> GetLegacyStormNamesAsync(string username, string password, string region, CancellationToken cancellationToken)
    {
        if (!string.Equals(username, "demo", StringComparison.Ordinal) || !string.Equals(password, "demo", StringComparison.Ordinal))
        {
            return
            [
                new LegacyStormNameItem { ERROR_DESCRIPTION = "Incorrect username and password" },
                new LegacyStormNameItem(),
            ];
        }

        var storms = await repository.GetStormNamesAsync(region, activeOnly: false, cancellationToken).ConfigureAwait(false);
        return storms.Select(storm => new LegacyStormNameItem
        {
            NAME = storm.Name,
            YEAR = storm.Year.ToString(CultureInfo.InvariantCulture),
            REGION = storm.Region,
            ERROR_DESCRIPTION = string.Empty,
        }).ToArray();
    }

    public async Task<DataSet> GetStormsDatasetAsync(string username, string password, string stormsToDownload, string region, CancellationToken cancellationToken)
    {
        var storms = await GetStormsAsync(stormsToDownload, region, withImageLinks: false, activeOnly: false, lastCoordinateOnly: false, omitForecastsToo: false, cancellationToken).ConfigureAwait(false);
        var dataSet = new DataSet();

        foreach (var storm in storms)
        {
            var table = dataSet.Tables.Add(storm.NameYear);
            table.Columns.Add("Region");
            table.Columns.Add("StormName_StormYear");
            table.Columns.Add("StormName");
            table.Columns.Add("StormYear");
            table.Columns.Add("latitude");
            table.Columns.Add("longitude");
            table.Columns.Add("wind_speed");
            table.Columns.Add("speed_travel");
            table.Columns.Add("pressure");
            table.Columns.Add("direction", typeof(int));
            table.Columns.Add("utcoffset", typeof(string));
            table.Columns.Add("datetime", typeof(DateTime));

            foreach (var coordinate in storm.Coordinates)
            {
                var row = table.NewRow();
                row["StormName_StormYear"] = storm.NameYear.ToUpperInvariant();
                row["Region"] = storm.Region;
                row["StormName"] = storm.Name.ToUpperInvariant();
                row["StormYear"] = storm.Year.ToString(CultureInfo.InvariantCulture);
                row["latitude"] = coordinate.Latitude.ToString(CultureInfo.InvariantCulture);
                row["longitude"] = coordinate.Longitude.ToString(CultureInfo.InvariantCulture);
                row["wind_speed"] = coordinate.WindSpeed.ToString(CultureInfo.InvariantCulture);
                row["speed_travel"] = coordinate.SpeedTravel.ToString(CultureInfo.InvariantCulture);
                row["pressure"] = coordinate.Pressure.ToString(CultureInfo.InvariantCulture);
                row["direction"] = coordinate.Direction;
                row["datetime"] = coordinate.CoordinateDate;
                row["utcoffset"] = coordinate.UtcOffset;
                table.Rows.Add(row);
            }
        }

        return dataSet;
    }
}
