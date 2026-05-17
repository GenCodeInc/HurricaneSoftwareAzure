using TropicalStorms.Api.Models.Website;
using TTENET.TTEBusiness.Core.Models;

namespace TropicalStorms.Api.Services;

public interface IWebsiteExperienceService
{
    Task<ReturnMessage> RecoverRegistrationAsync(RecoverRegistrationRequest request, CancellationToken cancellationToken);

    Task<ReturnMessage> RecoverRegistrationWithAcsAsync(RecoverRegistrationRequest request, CancellationToken cancellationToken);

    Task<WebsiteMessageResponse> SendContactAsync(ContactWebsiteRequest request, CancellationToken cancellationToken);

    Task<WebsiteMessageResponse> ConfirmAlertAsync(ConfirmWebsiteAlertRequest request, CancellationToken cancellationToken);

    Task<WebsiteOrderQuoteResponse> QuoteAsync(WebsiteOrderQuoteRequest request, CancellationToken cancellationToken);

    Task<CreatePayPalWebsiteOrderResponse> CreatePayPalOrderAsync(WebsiteCheckoutRequest request, CancellationToken cancellationToken);

    Task<CapturePayPalWebsiteOrderResponse> CapturePayPalOrderAsync(CaptureWebsiteCheckoutRequest request, CancellationToken cancellationToken);
}