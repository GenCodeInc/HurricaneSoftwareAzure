namespace TropicalStorms.Api.Services;

public sealed class WebsitePayPalOptions
{
    public bool Enabled { get; set; }

    public string BaseUrl { get; set; } = "https://api-m.sandbox.paypal.com";

    public string ClientId { get; set; } = string.Empty;

    public string ClientSecret { get; set; } = string.Empty;

    public string BrandName { get; set; } = "HurricaneSoftware.com";
}