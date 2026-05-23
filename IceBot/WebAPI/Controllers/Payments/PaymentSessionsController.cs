using Application.Payments.PaymentSessions.Requests;
using Application.Payments.PaymentSessions.Services;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WebAPI.Controllers.Payments;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}")]
public sealed class PaymentSessionsController : ControllerBase
{
    private readonly PaymentSessionService _paymentSessionService;

    public PaymentSessionsController(PaymentSessionService paymentSessionService)
    {
        _paymentSessionService = paymentSessionService;
    }

    [HttpPost("orders/{orderId:guid}/payment-sessions")]
    [AllowAnonymous]
    public async Task<IActionResult> CreatePaymentSession(
        Guid orderId,
        [FromBody] CreatePaymentSessionRequest request,
        CancellationToken cancellationToken)
    {
        request ??= new CreatePaymentSessionRequest();

        if (string.IsNullOrWhiteSpace(request.IdempotencyKey) &&
            Request.Headers.TryGetValue("Idempotency-Key", out var idempotencyKey))
        {
            request.IdempotencyKey = idempotencyKey.ToString();
        }

        var result = await _paymentSessionService.CreatePaymentSessionAsync(orderId, request, cancellationToken);
        return StatusCode(result.StatusCode, result);
    }

    [HttpGet("orders/{orderId:guid}/payment-status")]
    [AllowAnonymous]
    public async Task<IActionResult> GetOrderPaymentStatus(Guid orderId, CancellationToken cancellationToken)
    {
        var result = await _paymentSessionService.GetOrderPaymentStatusAsync(orderId, cancellationToken);
        return StatusCode(result.StatusCode, result);
    }

    [HttpGet("payment-transactions/{paymentTransactionId:guid}")]
    [Authorize(Policy = "payments.manage")]
    public async Task<IActionResult> GetPaymentTransactionStatus(Guid paymentTransactionId, CancellationToken cancellationToken)
    {
        var result = await _paymentSessionService.GetPaymentTransactionStatusAsync(paymentTransactionId, cancellationToken);
        return StatusCode(result.StatusCode, result);
    }
}
