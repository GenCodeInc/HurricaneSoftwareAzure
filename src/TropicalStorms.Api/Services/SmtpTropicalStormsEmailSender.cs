using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Options;

namespace TropicalStorms.Api.Services;

public sealed class SmtpTropicalStormsEmailSender(IOptions<TropicalStormsEmailOptions> options) : ITropicalStormsEmailSender
{
    public bool IsEnabled => options.Value.Enabled &&
        !string.IsNullOrWhiteSpace(options.Value.Host) &&
        !string.IsNullOrWhiteSpace(options.Value.FromAddress);

    public Task SendAsync(string toAddress, string subject, string body, CancellationToken cancellationToken)
    {
        if (!IsEnabled || string.IsNullOrWhiteSpace(toAddress))
        {
            return Task.CompletedTask;
        }

        using var message = new MailMessage
        {
            From = string.IsNullOrWhiteSpace(options.Value.FromName)
                ? new MailAddress(options.Value.FromAddress)
                : new MailAddress(options.Value.FromAddress, options.Value.FromName),
            Subject = subject,
            Body = body,
        };
        message.To.Add(new MailAddress(toAddress));

        using var client = new SmtpClient(options.Value.Host, options.Value.Port)
        {
            EnableSsl = options.Value.UseSsl,
        };

        if (!string.IsNullOrWhiteSpace(options.Value.UserName))
        {
            client.Credentials = new NetworkCredential(options.Value.UserName, options.Value.Password);
        }

        return client.SendMailAsync(message, cancellationToken);
    }
}
