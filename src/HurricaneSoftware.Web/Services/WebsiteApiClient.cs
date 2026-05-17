using System.Net.Http.Json;
using System.Text.Json;
using HurricaneSoftware.Web.Models;

namespace HurricaneSoftware.Web.Services;

public sealed class WebsiteApiClient(HttpClient httpClient)
{
    public Task<ApiResult<ReturnMessage>> RecoverRegistrationAsync(RecoverRegistrationRequest request, CancellationToken cancellationToken = default)
        => PostAsync<RecoverRegistrationRequest, ReturnMessage>("api/website/registration/recover/acs", request, cancellationToken);

    public Task<ApiResult<WebsiteMessageResponse>> ContactAsync(ContactWebsiteRequest request, CancellationToken cancellationToken = default)
        => PostAsync<ContactWebsiteRequest, WebsiteMessageResponse>("api/website/contact", request, cancellationToken);

    public Task<ApiResult<WebsiteMessageResponse>> ConfirmAlertAsync(ConfirmWebsiteAlertRequest request, CancellationToken cancellationToken = default)
        => PostAsync<ConfirmWebsiteAlertRequest, WebsiteMessageResponse>("api/website/alerts/confirm", request, cancellationToken);

    public Task<ApiResult<WebsiteOrderQuoteResponse>> QuoteAsync(WebsiteOrderQuoteRequest request, CancellationToken cancellationToken = default)
        => PostAsync<WebsiteOrderQuoteRequest, WebsiteOrderQuoteResponse>("api/website/orders/quote", request, cancellationToken);

    public Task<ApiResult<CreatePayPalWebsiteOrderResponse>> CreatePayPalOrderAsync(WebsiteCheckoutRequest request, CancellationToken cancellationToken = default)
        => PostAsync<WebsiteCheckoutRequest, CreatePayPalWebsiteOrderResponse>("api/website/orders/paypal/create", request, cancellationToken);

    public Task<ApiResult<CapturePayPalWebsiteOrderResponse>> CapturePayPalOrderAsync(CaptureWebsiteCheckoutRequest request, CancellationToken cancellationToken = default)
        => PostAsync<CaptureWebsiteCheckoutRequest, CapturePayPalWebsiteOrderResponse>("api/website/orders/paypal/capture", request, cancellationToken);

    private async Task<ApiResult<TResponse>> PostAsync<TRequest, TResponse>(string uri, TRequest request, CancellationToken cancellationToken)
    {
        using var response = await httpClient.PostAsJsonAsync(uri, request, cancellationToken).ConfigureAwait(false);
        if (response.IsSuccessStatusCode)
        {
            var payload = await response.Content.ReadFromJsonAsync<TResponse>(cancellationToken: cancellationToken).ConfigureAwait(false);
            return ApiResult<TResponse>.FromSuccess(payload);
        }

        return ApiResult<TResponse>.FromError(await ReadProblemMessageAsync(response, cancellationToken).ConfigureAwait(false));
    }

    private static async Task<string> ReadProblemMessageAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        var body = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        if (string.IsNullOrWhiteSpace(body))
        {
            return $"Request failed with status code {(int)response.StatusCode}.";
        }

        try
        {
            using var document = JsonDocument.Parse(body);
            if (document.RootElement.TryGetProperty("detail", out var detail) && detail.ValueKind == JsonValueKind.String)
            {
                return detail.GetString() ?? body;
            }

            if (document.RootElement.TryGetProperty("title", out var title) && title.ValueKind == JsonValueKind.String)
            {
                return title.GetString() ?? body;
            }
        }
        catch (JsonException)
        {
        }

        return body;
    }
}
