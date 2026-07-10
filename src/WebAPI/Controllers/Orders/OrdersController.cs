using Application.Orders.PlaceOrder.Commands;
using Application.Orders.PlaceOrder.Queries;
using Application.Orders.PlaceOrder.Requests;
using Application.Payments.PaymentSessions.Commands;
using Application.Payments.PaymentSessions.Queries;
using Application.Payments.PaymentSessions.Requests;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebAPI.Configuration.Security;

namespace WebAPI.Controllers.Orders;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/orders")]
public sealed class OrdersController : ControllerBase
{
    private readonly PlaceOrderCommandHandler _placeOrderHandler;
    private readonly GetOrderStatusQueryHandler _getOrderStatusHandler;
    private readonly CancelPendingOrderCommandHandler _cancelOrderHandler;
    private readonly CreatePaymentSessionCommandHandler _createPaymentSessionHandler;
    private readonly GetOrderPaymentStatusQueryHandler _getOrderPaymentStatusHandler;
    private readonly IPublicOrderAccessTokenService _publicOrderAccess;

    public OrdersController(
        PlaceOrderCommandHandler placeOrderHandler,
        GetOrderStatusQueryHandler getOrderStatusHandler,
        CancelPendingOrderCommandHandler cancelOrderHandler,
        CreatePaymentSessionCommandHandler createPaymentSessionHandler,
        GetOrderPaymentStatusQueryHandler getOrderPaymentStatusHandler,
        IPublicOrderAccessTokenService publicOrderAccess)
    {
        _placeOrderHandler = placeOrderHandler;
        _getOrderStatusHandler = getOrderStatusHandler;
        _cancelOrderHandler = cancelOrderHandler;
        _createPaymentSessionHandler = createPaymentSessionHandler;
        _getOrderPaymentStatusHandler = getOrderPaymentStatusHandler;
        _publicOrderAccess = publicOrderAccess;
    }

    /// <summary>
    /// Places a new kiosk order.
    /// </summary>
    /// <param name="request">The order details.</param>
    /// <param name="idempotencyKey">A unique key to prevent duplicate order creations.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The created order status.</returns>
    [HttpPost]
    [AllowAnonymous]
    public async Task<IActionResult> PlaceOrder(
        [FromBody] PlaceOrderRequest request,
        [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey,
        CancellationToken cancellationToken)
    {
        request ??= new PlaceOrderRequest();

        var command = new PlaceOrderCommand { Request = request, IdempotencyKey = idempotencyKey };
        var result = await _placeOrderHandler.HandleAsync(command, cancellationToken);
        IssueOrderAccessToken(result);
        return StatusCode(result.StatusCode, result);
    }

    [HttpGet("{orderId:guid}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetOrderStatus(
        Guid orderId,
        [FromHeader(Name = "Order-Access-Token")] string? orderAccessToken,
        CancellationToken cancellationToken)
    {
        if (!CanAccessOrder(orderAccessToken, orderId)) return Unauthorized();
        var query = new GetOrderStatusQuery { OrderId = orderId };
        var result = await _getOrderStatusHandler.HandleAsync(query, cancellationToken);
        return StatusCode(result.StatusCode, result);
    }

    /// <summary>
    /// Creates a payment session for an order.
    /// </summary>
    /// <param name="orderId">The ID of the order.</param>
    /// <param name="request">The selected payment method and client-displayed total.</param>
    /// <param name="idempotencyKey">A unique key to prevent duplicate payment session creations.</param>
    /// <param name="orderAccessToken">The bearer token returned when the order was created.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The created payment session details.</returns>
    [HttpPost("{orderId:guid}/payment-sessions")]
    [AllowAnonymous]
    public async Task<IActionResult> CreatePaymentSession(
        Guid orderId,
        [FromBody] CreatePaymentSessionRequest request,
        [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey,
        [FromHeader(Name = "Order-Access-Token")] string? orderAccessToken,
        CancellationToken cancellationToken)
    {
        if (!CanAccessOrder(orderAccessToken, orderId)) return Unauthorized();
        var command = new CreatePaymentSessionCommand
        {
            OrderId = orderId,
            IdempotencyKey = idempotencyKey,
            Request = request
        };
        var result = await _createPaymentSessionHandler.HandleAsync(command, cancellationToken);
        return StatusCode(result.StatusCode, result);
    }

    [HttpGet("{orderId:guid}/payment-status")]
    [AllowAnonymous]
    public async Task<IActionResult> GetOrderPaymentStatus(
        Guid orderId,
        [FromHeader(Name = "Order-Access-Token")] string? orderAccessToken,
        CancellationToken cancellationToken)
    {
        if (!CanAccessOrder(orderAccessToken, orderId)) return Unauthorized();
        var query = new GetOrderPaymentStatusQuery { OrderId = orderId };
        var result = await _getOrderPaymentStatusHandler.HandleAsync(query, cancellationToken);
        return StatusCode(result.StatusCode, result);
    }

    [HttpPost("{orderId:guid}/cancel")]
    [AllowAnonymous]
    public async Task<IActionResult> CancelPendingOrder(
        Guid orderId,
        [FromBody] CancelPendingOrderRequest request,
        [FromHeader(Name = "Order-Access-Token")] string? orderAccessToken,
        CancellationToken cancellationToken)
    {
        if (!CanAccessOrder(orderAccessToken, orderId)) return Unauthorized();
        request ??= new CancelPendingOrderRequest();

        var command = new CancelPendingOrderCommand { OrderId = orderId, Request = request };
        var result = await _cancelOrderHandler.HandleAsync(command, cancellationToken);
        return StatusCode(result.StatusCode, result);
    }

    private bool CanAccessOrder(string? token, Guid orderId) => _publicOrderAccess.CanAccess(token, orderId);

    private void IssueOrderAccessToken(Application.Shared.Wrappers.ApiResult<Application.Orders.PlaceOrder.Results.OrderResult> result)
    {
        if (result.Succeeded && result.Data is not null)
        {
            result.Data.OrderAccessToken = _publicOrderAccess.Issue(result.Data.Id, result.Data.KioskId);
        }
    }
}
