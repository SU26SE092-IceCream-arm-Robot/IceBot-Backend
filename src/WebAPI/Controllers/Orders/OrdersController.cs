using Application.Orders.PlaceOrder.Requests;
using Application.Orders.PlaceOrder.Services;
using Application.Payments.PaymentSessions.Requests;
using Application.Payments.PaymentSessions.Services;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WebAPI.Controllers.Orders;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/orders")]
public sealed class OrdersController : ControllerBase
{
    private readonly PlaceOrderService _placeOrderService;
    private readonly PaymentSessionService _paymentSessionService;

    public OrdersController(
        PlaceOrderService placeOrderService,
        PaymentSessionService paymentSessionService)
    {
        _placeOrderService = placeOrderService;
        _paymentSessionService = paymentSessionService;
    }

    [HttpPost]
    [AllowAnonymous]
    public async Task<IActionResult> PlaceOrder(
        [FromBody] PlaceOrderRequest request,
        CancellationToken cancellationToken)
    {
        request ??= new PlaceOrderRequest();

        if (string.IsNullOrWhiteSpace(request.IdempotencyKey) &&
            Request.Headers.TryGetValue("Idempotency-Key", out var idempotencyKey))
        {
            request.IdempotencyKey = idempotencyKey.ToString();
        }

        var result = await _placeOrderService.PlaceOrderAsync(request, cancellationToken);
        return StatusCode(result.StatusCode, result);
    }

    [HttpGet("{orderId:guid}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetOrderStatus(Guid orderId, CancellationToken cancellationToken)
    {
        var result = await _placeOrderService.GetOrderStatusAsync(orderId, cancellationToken);
        return StatusCode(result.StatusCode, result);
    }

    [HttpPost("{orderId:guid}/payment-sessions")]
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

    [HttpGet("{orderId:guid}/payment-status")]
    [AllowAnonymous]
    public async Task<IActionResult> GetOrderPaymentStatus(Guid orderId, CancellationToken cancellationToken)
    {
        var result = await _paymentSessionService.GetOrderPaymentStatusAsync(orderId, cancellationToken);
        return StatusCode(result.StatusCode, result);
    }

    [HttpPost("{orderId:guid}/cancel")]
    [AllowAnonymous]
    public async Task<IActionResult> CancelPendingOrder(
        Guid orderId,
        [FromBody] CancelPendingOrderRequest request,
        CancellationToken cancellationToken)
    {
        request ??= new CancelPendingOrderRequest();

        var result = await _placeOrderService.CancelPendingOrderAsync(orderId, request, cancellationToken);
        return StatusCode(result.StatusCode, result);
    }
}
