using System.ComponentModel.DataAnnotations;

namespace TropicalStorms.Api.Models.Website;

public sealed class RecoverRegistrationRequest
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;
}

public sealed class ContactWebsiteRequest
{
    [Required]
    [StringLength(120)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    public string ConfirmEmail { get; set; } = string.Empty;

    [Required]
    [StringLength(80)]
    public string OperatingSystem { get; set; } = string.Empty;

    [Required]
    [StringLength(4000)]
    public string Message { get; set; } = string.Empty;

    [StringLength(80)]
    public string? UserId { get; set; }

    [StringLength(80)]
    public string? RegistrationNumber { get; set; }
}

public sealed class ConfirmWebsiteAlertRequest
{
    [Range(1, int.MaxValue)]
    public int ConfirmId { get; set; }
}

public class WebsiteOrderQuoteRequest
{
    public bool IsRenewal { get; set; }

    [EmailAddress]
    public string? RenewalEmail { get; set; }

    public bool IsCorporate { get; set; }

    [Range(1, 3)]
    public int TermYears { get; set; } = 1;

    public bool IncludeFlashDrive { get; set; }

    public int? SeatCount { get; set; }

    [StringLength(40)]
    public string? PromoCode { get; set; }
}

public sealed class WebsiteCheckoutRequest : WebsiteOrderQuoteRequest
{
    [Required]
    [StringLength(60)]
    public string FirstName { get; set; } = string.Empty;

    [Required]
    [StringLength(60)]
    public string LastName { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    public string ConfirmEmail { get; set; } = string.Empty;

    [Required]
    [StringLength(120)]
    public string Address1 { get; set; } = string.Empty;

    [StringLength(120)]
    public string? Address2 { get; set; }

    [Required]
    [StringLength(80)]
    public string City { get; set; } = string.Empty;

    [Required]
    [StringLength(80)]
    public string StateOrProvince { get; set; } = string.Empty;

    [Required]
    [StringLength(24)]
    public string PostalCode { get; set; } = string.Empty;

    [Required]
    [StringLength(2, MinimumLength = 2)]
    public string CountryCode { get; set; } = "US";

    [Required]
    [StringLength(80)]
    public string CountryName { get; set; } = "United States";

    [Required]
    [Url]
    public string ReturnUrl { get; set; } = string.Empty;

    [Required]
    [Url]
    public string CancelUrl { get; set; } = string.Empty;
}

public sealed class CaptureWebsiteCheckoutRequest
{
    [Required]
    public string PayPalOrderId { get; set; } = string.Empty;

    [Required]
    public WebsiteCheckoutRequest Checkout { get; set; } = new();
}

public sealed class WebsiteMessageResponse
{
    public string Message { get; set; } = string.Empty;
}

public sealed class WebsiteOrderQuoteResponse
{
    public decimal Amount { get; set; }

    public string Currency { get; set; } = "USD";

    public string Description { get; set; } = string.Empty;

    public bool IsRenewal { get; set; }

    public bool IsCorporate { get; set; }

    public int TermYears { get; set; }

    public int? SeatCount { get; set; }
}

public sealed class CreatePayPalWebsiteOrderResponse
{
    public string OrderId { get; set; } = string.Empty;

    public string ApprovalUrl { get; set; } = string.Empty;

    public WebsiteOrderQuoteResponse Quote { get; set; } = new();
}

public sealed class CapturePayPalWebsiteOrderResponse
{
    public string TransactionId { get; set; } = string.Empty;

    public string Message { get; set; } = string.Empty;

    public bool IsRenewal { get; set; }

    public string UserId { get; set; } = string.Empty;

    public string RegistrationNumber { get; set; } = string.Empty;

    public DateTime ExpirationDateUtc { get; set; }
}