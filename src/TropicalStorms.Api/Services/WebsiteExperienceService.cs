using System.Globalization;
using System.Net.Mail;
using System.Text;
using Microsoft.Extensions.Options;
using TropicalStorms.Api.Models.Website;
using TTENET.TTEBusiness.Core.Models;
using TTENET.TTEBusiness.Core.Services;
using TTENET.TTEBusiness.Core.Utilities;

namespace TropicalStorms.Api.Services;

public sealed class WebsiteExperienceService(
    ITropicalStormsRepository repository,
    ITropicalStormsFacade facade,
    IWebsiteRegistrationRecoverySender acsRecoverySender,
    IWebsitePaymentsClient paymentsClient,
    IOptions<TropicalStormsEmailOptions> emailOptions,
    IOptions<WebsitePricingOptions> pricingOptions,
    ILogger<WebsiteExperienceService> logger) : IWebsiteExperienceService
{
    private const int DefaultReferredBy = 2;

    public Task<ReturnMessage> RecoverRegistrationAsync(RecoverRegistrationRequest request, CancellationToken cancellationToken)
        => facade.RetrieveRegistrationAsync(request.Email.Trim(), cancellationToken);

    public async Task<ReturnMessage> RecoverRegistrationWithAcsAsync(RecoverRegistrationRequest request, CancellationToken cancellationToken)
    {
        var normalizedEmail = request.Email.Trim();
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
            logger.LogError(exception, "Failed to retrieve registration for ACS recovery email to {Email}.", normalizedEmail);
            return new ReturnMessage(2, "There was an error retrieving your registration information. Please try again shortly.");
        }

        if (registration is null || string.IsNullOrWhiteSpace(registration.UserId))
        {
            return new ReturnMessage(1, $"No registration code was found for email address {normalizedEmail}");
        }

        if (!acsRecoverySender.IsEnabled)
        {
            return new ReturnMessage(2, "Registration recovery email delivery is not configured yet.");
        }

        try
        {
            await acsRecoverySender.SendRegistrationRecoveryAsync(normalizedEmail, registration, cancellationToken).ConfigureAwait(false);
            return new ReturnMessage(0, $"Your registration information has been sent to {normalizedEmail}");
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Failed to send ACS registration recovery email to {Email}.", normalizedEmail);
            return new ReturnMessage(2, $"We found registration information for {normalizedEmail}, but there was a problem sending the email. Please try again shortly.");
        }
    }

    public async Task<WebsiteMessageResponse> SendContactAsync(ContactWebsiteRequest request, CancellationToken cancellationToken)
    {
        ValidateMatchingEmails(request.Email, request.ConfirmEmail, "Email and confirm email must match.");
        ValidateConfiguredEmail();

        var adminAddress = emailOptions.Value.AdminAddress;
        if (string.IsNullOrWhiteSpace(adminAddress))
        {
            throw new InvalidOperationException("The website support destination email address is not configured.");
        }

        var body = new StringBuilder()
            .AppendLine($"Name: {request.Name.Trim()}")
            .AppendLine($"Email: {request.Email.Trim()}")
            .AppendLine($"OS: {request.OperatingSystem.Trim()}")
            .AppendLine()
            .AppendLine($"UserID: {request.UserId?.Trim()}")
            .AppendLine($"RegistrationNumber: {request.RegistrationNumber?.Trim()}")
            .AppendLine()
            .AppendLine(request.Message.Trim())
            .ToString();

        var htmlBody = $"""
            <p><strong>Name:</strong> {request.Name.Trim()}</p>
            <p><strong>Email:</strong> {request.Email.Trim()}</p>
            <p><strong>OS:</strong> {request.OperatingSystem.Trim()}</p>
            <p><strong>UserID:</strong> {request.UserId?.Trim()}</p>
            <p><strong>RegistrationNumber:</strong> {request.RegistrationNumber?.Trim()}</p>
            <p>{request.Message.Trim()}</p>
            """;

        await acsRecoverySender.SendAsync(adminAddress, "Tracking The Eye website contact", body, htmlBody, cancellationToken).ConfigureAwait(false);

        return new WebsiteMessageResponse
        {
            Message = "Your message has been sent."
        };
    }

    public async Task<WebsiteMessageResponse> ConfirmAlertAsync(ConfirmWebsiteAlertRequest request, CancellationToken cancellationToken)
    {
        var alerts = await repository.GetAlertsAsync(request.ConfirmId, null, null, null, null, null, cancellationToken).ConfigureAwait(false);
        var alert = alerts.SingleOrDefault();
        if (alert is null)
        {
            throw new ArgumentException("The alert confirmation link is invalid or has expired.");
        }

        if (!alert.Confirmed)
        {
            await repository.UpdateAlertConfirmationAsync(alert.Id, confirmed: true, cancellationToken).ConfigureAwait(false);
        }

        return new WebsiteMessageResponse
        {
            Message = $"You are now confirmed to get alerts at {alert.Value}."
        };
    }

    public Task<WebsiteOrderQuoteResponse> QuoteAsync(WebsiteOrderQuoteRequest request, CancellationToken cancellationToken)
    {
        _ = cancellationToken;
        return Task.FromResult(BuildQuote(request));
    }

    public async Task<CreatePayPalWebsiteOrderResponse> CreatePayPalOrderAsync(WebsiteCheckoutRequest request, CancellationToken cancellationToken)
    {
        ValidateCheckoutRequest(request);

        if (request.IsRenewal)
        {
            var renewalRegistration = await repository.GetRegistrationByEmailAsync(request.RenewalEmail!.Trim(), cancellationToken).ConfigureAwait(false);
            if (renewalRegistration is null)
            {
                throw new ArgumentException($"No registration was found for {request.RenewalEmail.Trim()}.");
            }
        }

        var quote = BuildQuote(request);
        var paymentOrder = await paymentsClient.CreateOrderAsync(
            quote.Amount,
            quote.Description,
            request.ReturnUrl.Trim(),
            request.CancelUrl.Trim(),
            cancellationToken).ConfigureAwait(false);

        return new CreatePayPalWebsiteOrderResponse
        {
            OrderId = paymentOrder.OrderId,
            ApprovalUrl = paymentOrder.ApprovalUrl,
            Quote = quote,
        };
    }

    public async Task<CapturePayPalWebsiteOrderResponse> CapturePayPalOrderAsync(CaptureWebsiteCheckoutRequest request, CancellationToken cancellationToken)
    {
        if (request.Checkout is null)
        {
            throw new ArgumentException("Checkout details are required.");
        }

        ValidateCheckoutRequest(request.Checkout);
        var quote = BuildQuote(request.Checkout);
        var capture = await paymentsClient.CaptureOrderAsync(request.PayPalOrderId.Trim(), cancellationToken).ConfigureAwait(false);
        if (!string.Equals(capture.Currency, quote.Currency, StringComparison.OrdinalIgnoreCase) || capture.Amount != quote.Amount)
        {
            throw new InvalidOperationException("The captured PayPal amount did not match the expected order total.");
        }

        var completion = await FinalizeRegistrationAsync(request.Checkout, capture.TransactionId, quote, cancellationToken).ConfigureAwait(false);
        return completion;
    }

    private async Task<CapturePayPalWebsiteOrderResponse> FinalizeRegistrationAsync(WebsiteCheckoutRequest request, string transactionId, WebsiteOrderQuoteResponse quote, CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        RegistrationRecordItem registration;
        var isRenewal = request.IsRenewal;

        if (isRenewal)
        {
            registration = await repository.GetRegistrationByEmailAsync(request.RenewalEmail!.Trim(), cancellationToken).ConfigureAwait(false)
                ?? throw new ArgumentException($"No registration was found for {request.RenewalEmail.Trim()}.");

            // The legacy site extended the current expiration if the user renewed early and restarted from now when expired.
            registration.DateExpire = registration.DateExpire < now
                ? now.AddYears(quote.TermYears)
                : registration.DateExpire.AddYears(quote.TermYears);
            registration.Email = request.Email.Trim();
            registration.UserName = BuildFullName(request.FirstName, request.LastName);

            await repository.UpdateRegistrationAsync(registration, cancellationToken).ConfigureAwait(false);
        }
        else
        {
            var fullName = BuildFullName(request.FirstName, request.LastName);
            var userId = BuildWebsiteUserId(fullName);

            registration = new RegistrationRecordItem
            {
                UserName = fullName,
                UserId = userId,
                RegistrationNumber = RegistrationCodeUtility.GetRegCode(userId),
                Email = request.Email.Trim(),
                QtyOrdered = 1,
                ReferredBy = DefaultReferredBy,
                DateRegistered = now,
                DateExpire = now.AddYears(quote.TermYears),
                CellPhoneAlert = string.Empty,
                EmailAlert = request.Email.Trim(),
            };

            await repository.CreateRegistrationAsync(registration, cancellationToken).ConfigureAwait(false);
        }

        await SendRegistrationEmailsAsync(request, registration, quote, transactionId, cancellationToken).ConfigureAwait(false);

        logger.LogInformation(
            "Completed website {OrderKind} order for {Email}. PayPal transaction {TransactionId} expires {ExpirationDate}.",
            isRenewal ? "renewal" : "new",
            registration.Email,
            transactionId,
            registration.DateExpire);

        return new CapturePayPalWebsiteOrderResponse
        {
            TransactionId = transactionId,
            Message = isRenewal
                ? "Your Tracking The Eye renewal is complete."
                : "Your Tracking The Eye registration is complete.",
            IsRenewal = isRenewal,
            UserId = registration.UserId,
            RegistrationNumber = registration.RegistrationNumber,
            ExpirationDateUtc = registration.DateExpire,
        };
    }

    private async Task SendRegistrationEmailsAsync(WebsiteCheckoutRequest request, RegistrationRecordItem registration, WebsiteOrderQuoteResponse quote, string transactionId, CancellationToken cancellationToken)
    {
        if (!acsRecoverySender.IsEnabled)
        {
            return;
        }

        var customerBody = new StringBuilder()
            .AppendLine(request.IsRenewal ? "Thank you for renewing Tracking The Eye." : "Thank you for registering Tracking The Eye.")
            .AppendLine()
            .AppendLine($"UserID: {registration.UserId}")
            .AppendLine($"Registration Code: {registration.RegistrationNumber}")
            .AppendLine($"Expires: {registration.DateExpire.ToString("u", CultureInfo.InvariantCulture)}")
            .AppendLine($"Transaction ID: {transactionId}")
            .AppendLine($"Amount: {quote.Amount.ToString("0.00", CultureInfo.InvariantCulture)} {quote.Currency}")
            .ToString();

        var customerHtmlBody = $"""
            <p>{(request.IsRenewal ? "Thank you for renewing Tracking The Eye." : "Thank you for registering Tracking The Eye.")}</p>
            <p><strong>UserID:</strong> {registration.UserId}</p>
            <p><strong>Registration Code:</strong> {registration.RegistrationNumber}</p>
            <p><strong>Expires:</strong> {registration.DateExpire.ToString("u", CultureInfo.InvariantCulture)}</p>
            <p><strong>Transaction ID:</strong> {transactionId}</p>
            <p><strong>Amount:</strong> {quote.Amount.ToString("0.00", CultureInfo.InvariantCulture)} {quote.Currency}</p>
            """;

        await acsRecoverySender.SendAsync(registration.Email, "Tracking The Eye registration", customerBody, customerHtmlBody, cancellationToken).ConfigureAwait(false);

        var adminAddress = emailOptions.Value.AdminAddress;
        if (!string.IsNullOrWhiteSpace(adminAddress))
        {
            var adminBody = new StringBuilder()
                .AppendLine($"Order type: {(request.IsRenewal ? "Renewal" : "New")}")
                .AppendLine($"Corporate: {request.IsCorporate}")
                .AppendLine($"Promo: {request.PromoCode}")
                .AppendLine($"Customer: {registration.UserName} <{registration.Email}>")
                .AppendLine($"Transaction ID: {transactionId}")
                .AppendLine($"Amount: {quote.Amount.ToString("0.00", CultureInfo.InvariantCulture)} {quote.Currency}")
                .AppendLine($"Address: {request.Address1} {request.Address2}, {request.City}, {request.StateOrProvince}, {request.PostalCode}, {request.CountryName}")
                .ToString();

            var adminHtmlBody = $"""
                <p><strong>Order type:</strong> {(request.IsRenewal ? "Renewal" : "New")}</p>
                <p><strong>Corporate:</strong> {request.IsCorporate}</p>
                <p><strong>Promo:</strong> {request.PromoCode}</p>
                <p><strong>Customer:</strong> {registration.UserName} &lt;{registration.Email}&gt;</p>
                <p><strong>Transaction ID:</strong> {transactionId}</p>
                <p><strong>Amount:</strong> {quote.Amount.ToString("0.00", CultureInfo.InvariantCulture)} {quote.Currency}</p>
                <p><strong>Address:</strong> {request.Address1} {request.Address2}, {request.City}, {request.StateOrProvince}, {request.PostalCode}, {request.CountryName}</p>
                """;

            await acsRecoverySender.SendAsync(adminAddress, "Tracking The Eye website order", adminBody, adminHtmlBody, cancellationToken).ConfigureAwait(false);

            if (request.IncludeFlashDrive)
            {
                await acsRecoverySender.SendAsync(adminAddress, "Tracking The Eye flash drive add-on", adminBody, adminHtmlBody, cancellationToken).ConfigureAwait(false);
            }
        }
    }

    private WebsiteOrderQuoteResponse BuildQuote(WebsiteOrderQuoteRequest request)
    {
        if (request.IsRenewal && string.IsNullOrWhiteSpace(request.RenewalEmail))
        {
            throw new ArgumentException("Renewal email is required for renewal orders.");
        }

        if (!request.IsCorporate && request.SeatCount.HasValue)
        {
            throw new ArgumentException("Seat count is only valid for corporate orders.");
        }

        if (request.IsCorporate && !request.SeatCount.HasValue)
        {
            throw new ArgumentException("Seat count is required for corporate orders.");
        }

        if (request.IsCorporate && request.TermYears != 1)
        {
            throw new ArgumentException("Corporate website orders currently support one-year terms only.");
        }

        var pricing = pricingOptions.Value;
        var amount = request.IsCorporate
            ? GetCorporateAmount(pricing, request.SeatCount!.Value)
            : request.IsRenewal
                ? (request.TermYears == 3 ? pricing.RenewalThreeYear : pricing.RenewalOneYear)
                : (request.TermYears == 3 ? pricing.PersonalThreeYear : pricing.PersonalOneYear);

        if (request.IncludeFlashDrive)
        {
            amount += pricing.FlashDrive;
        }

        return new WebsiteOrderQuoteResponse
        {
            Amount = amount,
            Currency = "USD",
            Description = BuildQuoteDescription(request),
            IsRenewal = request.IsRenewal,
            IsCorporate = request.IsCorporate,
            TermYears = request.IsCorporate ? 1 : request.TermYears,
            SeatCount = request.SeatCount,
        };
    }

    private static decimal GetCorporateAmount(WebsitePricingOptions pricing, int seatCount) => seatCount switch
    {
        25 => pricing.Corporate25Seats,
        50 => pricing.Corporate50Seats,
        100 => pricing.Corporate100Seats,
        250 => pricing.Corporate250Seats,
        500 => pricing.Corporate500Seats,
        1000 => pricing.Corporate1000Seats,
        _ => throw new ArgumentException("Corporate seat count must be one of 25, 50, 100, 250, 500, or 1000."),
    };

    private static string BuildQuoteDescription(WebsiteOrderQuoteRequest request)
    {
        if (request.IsCorporate)
        {
            return $"Tracking The Eye corporate subscription ({request.SeatCount} seats)";
        }

        return request.IsRenewal
            ? $"Tracking The Eye renewal ({request.TermYears}-year)"
            : $"Tracking The Eye subscription ({request.TermYears}-year)";
    }

    private static void ValidateCheckoutRequest(WebsiteCheckoutRequest request)
    {
        ValidateMatchingEmails(request.Email, request.ConfirmEmail, "Email and confirm email must match.");

        if (!MailAddress.TryCreate(request.Email.Trim(), out _))
        {
            throw new ArgumentException("A valid email address is required.");
        }

        if (!Uri.TryCreate(request.ReturnUrl, UriKind.Absolute, out _))
        {
            throw new ArgumentException("Return URL must be an absolute URL.");
        }

        if (!Uri.TryCreate(request.CancelUrl, UriKind.Absolute, out _))
        {
            throw new ArgumentException("Cancel URL must be an absolute URL.");
        }
    }

    private void ValidateConfiguredEmail()
    {
        if (!acsRecoverySender.IsEnabled)
        {
            throw new InvalidOperationException("Email delivery is not configured for the website API.");
        }
    }

    private static void ValidateMatchingEmails(string email, string confirmEmail, string message)
    {
        if (!string.Equals(email.Trim(), confirmEmail.Trim(), StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException(message);
        }
    }

    private static string BuildFullName(string firstName, string lastName)
        => string.Join(' ', new[] { firstName.Trim(), lastName.Trim() }.Where(static value => !string.IsNullOrWhiteSpace(value)));

    private static string BuildWebsiteUserId(string fullName)
    {
        // The legacy site generated an opaque user key before deriving the registration number.
        // The original helper lives outside this .NET 8 repo, so this preserves the same idea:
        // a stable, human-readable name stem plus a unique suffix for new website-originated accounts.
        var stem = new string(fullName.ToUpperInvariant().Where(char.IsLetterOrDigit).ToArray());
        if (string.IsNullOrWhiteSpace(stem))
        {
            stem = "TTEUSER";
        }

        if (stem.Length > 16)
        {
            stem = stem[..16];
        }

        return $"{stem}-{Guid.NewGuid():N}"[..(stem.Length + 9)];
    }
}