using TTENET.TTEBusiness.Core.Models;

namespace TropicalStorms.Api.Services;

public interface IWebsiteRegistrationRecoverySender
{
    bool IsEnabled { get; }

    Task SendRegistrationRecoveryAsync(string toAddress, RegistrationRecordItem registration, CancellationToken cancellationToken);

    Task SendAsync(string toAddress, string subject, string plainTextBody, string? htmlBody, CancellationToken cancellationToken);
}