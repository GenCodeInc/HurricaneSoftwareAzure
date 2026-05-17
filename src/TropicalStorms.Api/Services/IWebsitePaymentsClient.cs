namespace TropicalStorms.Api.Services;

public interface IWebsitePaymentsClient
{
    Task<WebsitePaymentOrder> CreateOrderAsync(decimal amount, string description, string returnUrl, string cancelUrl, CancellationToken cancellationToken);

    Task<WebsitePaymentCapture> CaptureOrderAsync(string orderId, CancellationToken cancellationToken);
}

public sealed class WebsitePaymentOrder
{
    public string OrderId { get; init; } = string.Empty;

    public string ApprovalUrl { get; init; } = string.Empty;
}

public sealed class WebsitePaymentCapture
{
    public string OrderId { get; init; } = string.Empty;

    public string TransactionId { get; init; } = string.Empty;

    public decimal Amount { get; init; }

    public string Currency { get; init; } = "USD";
}