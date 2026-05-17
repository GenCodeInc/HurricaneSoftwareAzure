using Microsoft.AspNetCore.Mvc;
using TropicalStorms.Api.Models.Website;
using TropicalStorms.Api.Services;

namespace TropicalStorms.Api.Controllers;

[ApiController]
[Route("api/website")]
public sealed class WebsiteController(IWebsiteExperienceService websiteService) : ControllerBase
{
    [HttpPost("registration/recover")]
    public async Task<IActionResult> RecoverRegistration([FromBody] RecoverRegistrationRequest request, CancellationToken cancellationToken)
        => Ok(await websiteService.RecoverRegistrationAsync(request, cancellationToken).ConfigureAwait(false));

    [HttpPost("registration/recover/acs")]
    public async Task<IActionResult> RecoverRegistrationWithAcs([FromBody] RecoverRegistrationRequest request, CancellationToken cancellationToken)
        => Ok(await websiteService.RecoverRegistrationWithAcsAsync(request, cancellationToken).ConfigureAwait(false));

    [HttpPost("contact")]
    public async Task<IActionResult> Contact([FromBody] ContactWebsiteRequest request, CancellationToken cancellationToken)
        => await ExecuteAsync(() => websiteService.SendContactAsync(request, cancellationToken)).ConfigureAwait(false);

    [HttpPost("alerts/confirm")]
    public async Task<IActionResult> ConfirmAlert([FromBody] ConfirmWebsiteAlertRequest request, CancellationToken cancellationToken)
        => await ExecuteAsync(() => websiteService.ConfirmAlertAsync(request, cancellationToken)).ConfigureAwait(false);

    [HttpPost("orders/quote")]
    public async Task<IActionResult> Quote([FromBody] WebsiteOrderQuoteRequest request, CancellationToken cancellationToken)
        => await ExecuteAsync(() => websiteService.QuoteAsync(request, cancellationToken)).ConfigureAwait(false);

    [HttpPost("orders/paypal/create")]
    public async Task<IActionResult> CreatePayPalOrder([FromBody] WebsiteCheckoutRequest request, CancellationToken cancellationToken)
        => await ExecuteAsync(() => websiteService.CreatePayPalOrderAsync(request, cancellationToken)).ConfigureAwait(false);

    [HttpPost("orders/paypal/capture")]
    public async Task<IActionResult> CapturePayPalOrder([FromBody] CaptureWebsiteCheckoutRequest request, CancellationToken cancellationToken)
        => await ExecuteAsync(() => websiteService.CapturePayPalOrderAsync(request, cancellationToken)).ConfigureAwait(false);

    private async Task<IActionResult> ExecuteAsync<T>(Func<Task<T>> action)
    {
        try
        {
            return Ok(await action().ConfigureAwait(false));
        }
        catch (ArgumentException exception)
        {
            return ValidationProblem(detail: exception.Message);
        }
        catch (InvalidOperationException exception)
        {
            return Problem(detail: exception.Message, statusCode: StatusCodes.Status503ServiceUnavailable);
        }
    }
}