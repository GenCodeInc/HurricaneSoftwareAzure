using System.ComponentModel.DataAnnotations;

namespace HurricaneSoftware.Web.Models;

public sealed class ApiResult<T>
{
    public bool Success { get; init; }

    public T? Value { get; init; }

    public string ErrorMessage { get; init; } = string.Empty;

    public static ApiResult<T> FromSuccess(T? value) => new() { Success = true, Value = value };

    public static ApiResult<T> FromError(string errorMessage) => new() { Success = false, ErrorMessage = errorMessage };
}

public sealed class ReturnMessage
{
    public int MessageNumber { get; set; }

    public string Message { get; set; } = string.Empty;
}

public sealed class WebsiteMessageResponse
{
    public string Message { get; set; } = string.Empty;
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

    public string? PromoCode { get; set; }
}

public sealed class WebsiteCheckoutRequest : WebsiteOrderQuoteRequest
{
    [Required]
    public string FirstName { get; set; } = string.Empty;

    [Required]
    public string LastName { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    [Compare(nameof(Email), ErrorMessage = "Email and confirm email must match.")]
    public string ConfirmEmail { get; set; } = string.Empty;

    [Required]
    public string Address1 { get; set; } = string.Empty;

    public string? Address2 { get; set; }

    [Required]
    public string City { get; set; } = string.Empty;

    [Required]
    public string StateOrProvince { get; set; } = string.Empty;

    [Required]
    public string PostalCode { get; set; } = string.Empty;

    [Required]
    [StringLength(2, MinimumLength = 2)]
    public string CountryCode { get; set; } = "US";

    [Required]
    public string CountryName { get; set; } = "United States";

    [Required]
    public string ReturnUrl { get; set; } = string.Empty;

    [Required]
    public string CancelUrl { get; set; } = string.Empty;
}

public sealed class RecoverRegistrationRequest
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;
}

public sealed class ContactWebsiteRequest
{
    [Required]
    public string Name { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    [Compare(nameof(Email), ErrorMessage = "Email and confirm email must match.")]
    public string ConfirmEmail { get; set; } = string.Empty;

    [Required]
    public string OperatingSystem { get; set; } = "Windows";

    [Required]
    public string Message { get; set; } = string.Empty;

    public string? UserId { get; set; }

    public string? RegistrationNumber { get; set; }
}

public sealed class ConfirmWebsiteAlertRequest
{
    public int ConfirmId { get; set; }
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

public sealed class CaptureWebsiteCheckoutRequest
{
    public string PayPalOrderId { get; set; } = string.Empty;

    public WebsiteCheckoutRequest Checkout { get; set; } = new();
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
