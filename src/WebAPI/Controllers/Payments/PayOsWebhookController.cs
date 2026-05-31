using Application.Payments.PaymentSessions.Requests;
using Application.Payments.PaymentSessions.Services;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WebAPI.Controllers.Payments;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/payments/payos")]
public sealed class PayOsWebhookController : ControllerBase
{
    private readonly PaymentSessionService _paymentSessionService;

    public PayOsWebhookController(PaymentSessionService paymentSessionService)
    {
        _paymentSessionService = paymentSessionService;
    }

    [HttpPost("webhook")]
    [AllowAnonymous]
    public async Task<IActionResult> HandleWebhook(CancellationToken cancellationToken)
    {
        using var reader = new StreamReader(Request.Body);
        var rawPayload = await reader.ReadToEndAsync(cancellationToken);

        var request = new HandlePaymentProviderNotificationRequest
        {
            RawPayload = rawPayload,
            Signature = Request.Headers.TryGetValue("x-payos-signature", out var signature)
                ? signature.ToString()
                : null
        };

        var result = await _paymentSessionService.HandleProviderNotificationAsync(request, cancellationToken);
        return StatusCode(result.StatusCode, result);
    }
}
