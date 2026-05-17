namespace TropicalStorms.Api.Services;

public sealed class WebsitePricingOptions
{
    public decimal PersonalOneYear { get; set; } = 19.95m;

    public decimal PersonalThreeYear { get; set; } = 49.85m;

    public decimal RenewalOneYear { get; set; } = 16.95m;

    public decimal RenewalThreeYear { get; set; } = 34.95m;

    public decimal Corporate25Seats { get; set; } = 474m;

    public decimal Corporate50Seats { get; set; } = 848m;

    public decimal Corporate100Seats { get; set; } = 1596m;

    public decimal Corporate250Seats { get; set; } = 3490m;

    public decimal Corporate500Seats { get; set; } = 5985m;

    public decimal Corporate1000Seats { get; set; } = 9970m;

    public decimal FlashDrive { get; set; } = 10m;
}