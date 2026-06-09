using Application.Payments.PaymentSessions.Requests;
using Application.Payments.PaymentSessions.Commands;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WebAPI.Controllers.Payments;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/payments/payos")]
public sealed class PayOsWebhookController : ControllerBase
{
    private readonly HandlePaymentProviderNotificationCommandHandler _notificationHandler;

    public PayOsWebhookController(HandlePaymentProviderNotificationCommandHandler notificationHandler)
    {
        _notificationHandler = notificationHandler;
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

        var command = new HandlePaymentProviderNotificationCommand { Request = request };
        var result = await _notificationHandler.HandleAsync(command, cancellationToken);
        return StatusCode(result.StatusCode, result);
    }
}
