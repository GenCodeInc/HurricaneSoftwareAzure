using System.Globalization;
using System.Text;
using Azure;
using Azure.Communication.Email;
using Microsoft.Extensions.Options;
using TTENET.TTEBusiness.Core.Models;

namespace TropicalStorms.Api.Services;

public sealed class AcsWebsiteRegistrationRecoverySender(
    IOptions<WebsiteAcsEmailOptions> options,
    ILogger<AcsWebsiteRegistrationRecoverySender> logger) : IWebsiteRegistrationRecoverySender, ITropicalStormsEmailSender
{
    public bool IsEnabled => options.Value.Enabled
        && !string.IsNullOrWhiteSpace(options.Value.ConnectionString)
        && !string.IsNullOrWhiteSpace(options.Value.SenderAddress);

    public async Task SendRegistrationRecoveryAsync(string toAddress, RegistrationRecordItem registration, CancellationToken cancellationToken)
    {
        var subject = "Tracking The Eye Registration Code";
        var plainTextBody = new StringBuilder()
            .AppendLine("Tracking The Eye registration recovery")
            .AppendLine()
            .AppendLine($"UserID: {registration.UserId}")
            .AppendLine($"RegistrationCode: {registration.RegistrationNumber}")
            .AppendLine($"DateExpire: {registration.DateExpire.ToString("u", CultureInfo.InvariantCulture)}")
            .ToString();
        var htmlBody = $"""
            <p>Tracking The Eye registration recovery</p>
            <p><strong>UserID:</strong> {registration.UserId}</p>
            <p><strong>Registration Code:</strong> {registration.RegistrationNumber}</p>
            <p><strong>Date Expire:</strong> {registration.DateExpire.ToString("u", CultureInfo.InvariantCulture)}</p>
            """;

        await SendAsync(toAddress, subject, plainTextBody, htmlBody, cancellationToken).ConfigureAwait(false);

        logger.LogInformation("Sent ACS registration recovery email to {Email}.", toAddress);
    }

    public async Task SendAsync(string toAddress, string subject, string plainTextBody, string? htmlBody, CancellationToken cancellationToken)
    {
        if (!IsEnabled)
        {
            throw new InvalidOperationException("Azure Communication Services email is not configured.");
        }

        var currentOptions = options.Value;
        var client = new EmailClient(currentOptions.ConnectionString);
        var content = new EmailContent(subject)
        {
            PlainText = plainTextBody,
        };

        if (!string.IsNullOrWhiteSpace(htmlBody))
        {
            content.Html = htmlBody;
        }

        var recipients = new EmailRecipients([new EmailAddress(toAddress)]);
        var message = new EmailMessage(currentOptions.SenderAddress, recipients, content);

        var operation = await client.SendAsync(WaitUntil.Completed, message, cancellationToken).ConfigureAwait(false);
        logger.LogInformation(
            "Sent ACS website email to {Email} with status {Status}.",
            toAddress,
            operation.Value.Status);
    }

    Task ITropicalStormsEmailSender.SendAsync(string toAddress, string subject, string body, CancellationToken cancellationToken)
        => SendAsync(toAddress, subject, body, null, cancellationToken);
}