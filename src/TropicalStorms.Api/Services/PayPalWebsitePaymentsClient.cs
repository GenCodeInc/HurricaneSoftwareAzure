using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;

namespace TropicalStorms.Api.Services;

public sealed class PayPalWebsitePaymentsClient(HttpClient httpClient, IOptions<WebsitePayPalOptions> options) : IWebsitePaymentsClient
{
    public async Task<WebsitePaymentOrder> CreateOrderAsync(decimal amount, string description, string returnUrl, string cancelUrl, CancellationToken cancellationToken)
    {
        var settings = options.Value;
        ValidateSettings(settings);

        var accessToken = await GetAccessTokenAsync(settings, cancellationToken).ConfigureAwait(false);
        using var request = new HttpRequestMessage(HttpMethod.Post, BuildUri(settings.BaseUrl, "/v2/checkout/orders"));
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        request.Content = JsonContent.Create(new
        {
            intent = "CAPTURE",
            purchase_units = new[]
            {
                new
                {
                    description,
                    amount = new
                    {
                        currency_code = "USD",
                        value = amount.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture)
                    }
                }
            },
            application_context = new
            {
                brand_name = settings.BrandName,
                user_action = "PAY_NOW",
                return_url = returnUrl,
                cancel_url = cancelUrl
            }
        });

        using var response = await httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();

        var payload = await response.Content.ReadFromJsonAsync<CreateOrderResponse>(cancellationToken: cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("PayPal create order response was empty.");

        var approvalUrl = payload.Links?.FirstOrDefault(static link => string.Equals(link.Rel, "approve", StringComparison.OrdinalIgnoreCase))?.Href;
        if (string.IsNullOrWhiteSpace(payload.Id) || string.IsNullOrWhiteSpace(approvalUrl))
        {
            throw new InvalidOperationException("PayPal create order response did not include an approval URL.");
        }

        return new WebsitePaymentOrder
        {
            OrderId = payload.Id,
            ApprovalUrl = approvalUrl,
        };
    }

    public async Task<WebsitePaymentCapture> CaptureOrderAsync(string orderId, CancellationToken cancellationToken)
    {
        var settings = options.Value;
        ValidateSettings(settings);

        var accessToken = await GetAccessTokenAsync(settings, cancellationToken).ConfigureAwait(false);
        using var request = new HttpRequestMessage(HttpMethod.Post, BuildUri(settings.BaseUrl, $"/v2/checkout/orders/{orderId}/capture"));
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        request.Content = JsonContent.Create(new { });

        using var response = await httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();

        var payload = await response.Content.ReadFromJsonAsync<CaptureOrderResponse>(cancellationToken: cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("PayPal capture response was empty.");

        var capture = payload.PurchaseUnits?
            .SelectMany(static unit => unit.Payments?.Captures ?? [])
            .FirstOrDefault(static item => string.Equals(item.Status, "COMPLETED", StringComparison.OrdinalIgnoreCase));

        if (capture?.Amount is null || string.IsNullOrWhiteSpace(capture.Id))
        {
            throw new InvalidOperationException("PayPal capture response did not contain a completed capture.");
        }

        return new WebsitePaymentCapture
        {
            OrderId = payload.Id ?? orderId,
            TransactionId = capture.Id,
            Amount = decimal.Parse(capture.Amount.Value ?? "0", System.Globalization.CultureInfo.InvariantCulture),
            Currency = capture.Amount.CurrencyCode ?? "USD",
        };
    }

    private async Task<string> GetAccessTokenAsync(WebsitePayPalOptions settings, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, BuildUri(settings.BaseUrl, "/v1/oauth2/token"));
        var basicToken = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{settings.ClientId}:{settings.ClientSecret}"));
        request.Headers.Authorization = new AuthenticationHeaderValue("Basic", basicToken);
        request.Content = new FormUrlEncodedContent([new KeyValuePair<string, string>("grant_type", "client_credentials")]);

        using var response = await httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();

        var payload = await response.Content.ReadFromJsonAsync<AccessTokenResponse>(cancellationToken: cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("PayPal token response was empty.");

        if (string.IsNullOrWhiteSpace(payload.AccessToken))
        {
            throw new InvalidOperationException("PayPal token response did not include an access token.");
        }

        return payload.AccessToken;
    }

    private static void ValidateSettings(WebsitePayPalOptions settings)
    {
        if (!settings.Enabled || string.IsNullOrWhiteSpace(settings.ClientId) || string.IsNullOrWhiteSpace(settings.ClientSecret))
        {
            throw new InvalidOperationException("PayPal checkout is not configured for the website API.");
        }
    }

    private static Uri BuildUri(string baseUrl, string relativePath)
    {
        var baseUri = new Uri(baseUrl.EndsWith('/') ? baseUrl : baseUrl + "/", UriKind.Absolute);
        return new Uri(baseUri, relativePath.TrimStart('/'));
    }

    private sealed class AccessTokenResponse
    {
        [JsonPropertyName("access_token")]
        public string? AccessToken { get; init; }
    }

    private sealed class CreateOrderResponse
    {
        public string? Id { get; init; }

        public List<LinkDescription>? Links { get; init; }
    }

    private sealed class CaptureOrderResponse
    {
        public string? Id { get; init; }

        [JsonPropertyName("purchase_units")]
        public List<PurchaseUnit>? PurchaseUnits { get; init; }
    }

    private sealed class PurchaseUnit
    {
        public PaymentsBlock? Payments { get; init; }
    }

    private sealed class PaymentsBlock
    {
        public List<CaptureItem>? Captures { get; init; }
    }

    private sealed class CaptureItem
    {
        public string? Id { get; init; }

        public string? Status { get; init; }

        public MoneyAmount? Amount { get; init; }
    }

    private sealed class MoneyAmount
    {
        [JsonPropertyName("currency_code")]
        public string? CurrencyCode { get; init; }

        public string? Value { get; init; }
    }

    private sealed class LinkDescription
    {
        public string? Href { get; init; }

        public string? Rel { get; init; }
    }
}