namespace TropicalStorms.Api.Services;

public interface ITropicalStormsEmailSender
{
    bool IsEnabled { get; }

    Task SendAsync(string toAddress, string subject, string body, CancellationToken cancellationToken);
}
