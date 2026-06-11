using Application.Orders.Management.Commands;
using Application.Orders.Management.Queries;
using Application.Orders.Management.Requests;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebAPI.Authorization;

namespace WebAPI.Controllers.Orders;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/management/orders")]
public sealed class ManagementOrdersController : ControllerBase
{
    private readonly ListManagementOrdersQueryHandler _listHandler;
    private readonly GetManagementOrderQueryHandler _getHandler;
    private readonly GetOrderStatusHistoryQueryHandler _statusHistoryHandler;
    private readonly CancelManagementOrderCommandHandler _cancelHandler;
    private readonly MarkOrderRefundRequiredCommandHandler _refundRequiredHandler;

    public ManagementOrdersController(
        ListManagementOrdersQueryHandler listHandler,
        GetManagementOrderQueryHandler getHandler,
        GetOrderStatusHistoryQueryHandler statusHistoryHandler,
        CancelManagementOrderCommandHandler cancelHandler,
        MarkOrderRefundRequiredCommandHandler refundRequiredHandler)
    {
        _listHandler = listHandler;
        _getHandler = getHandler;
        _statusHistoryHandler = statusHistoryHandler;
        _cancelHandler = cancelHandler;
        _refundRequiredHandler = refundRequiredHandler;
    }

    [HttpGet]
    [Authorize(Policy = "orders.view")]
    public async Task<IActionResult> ListOrders(
        [FromQuery] string? search,
        [FromQuery] Domain.Orders.Enums.OrderStatus? status,
        [FromQuery] Domain.Orders.Enums.PaymentStatus? paymentStatus,
        [FromQuery] Guid? organizationId,
        [FromQuery] Guid? storeId,
        [FromQuery] Guid? kioskId,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var query = new ListManagementOrdersQuery
        {
            UserContext = User.GetUserContext(),
            Search = search,
            Status = status,
            PaymentStatus = paymentStatus,
            OrganizationId = organizationId,
            StoreId = storeId,
            KioskId = kioskId,
            PageNumber = pageNumber,
            PageSize = pageSize
        };

        var result = await _listHandler.HandleAsync(query, cancellationToken);
        return StatusCode(result.StatusCode, result);
    }

    [HttpGet("{orderId:guid}")]
    [Authorize(Policy = "orders.view")]
    public async Task<IActionResult> GetOrder(Guid orderId, CancellationToken cancellationToken)
    {
        var query = new GetManagementOrderQuery
        {
            OrderId = orderId,
            UserContext = User.GetUserContext()
        };

        var result = await _getHandler.HandleAsync(query, cancellationToken);
        return StatusCode(result.StatusCode, result);
    }

    [HttpGet("{orderId:guid}/status-history")]
    [Authorize(Policy = "orders.view")]
    public async Task<IActionResult> GetOrderStatusHistory(
        Guid orderId,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var query = new GetOrderStatusHistoryQuery
        {
            OrderId = orderId,
            UserContext = User.GetUserContext(),
            PageNumber = pageNumber,
            PageSize = pageSize
        };

        var result = await _statusHistoryHandler.HandleAsync(query, cancellationToken);
        return StatusCode(result.StatusCode, result);
    }

    [HttpPatch("{orderId:guid}/cancel")]
    [Authorize(Policy = "orders.manage")]
    public async Task<IActionResult> CancelOrder(
        Guid orderId,
        [FromBody] ManagementOrderReasonRequest request,
        CancellationToken cancellationToken)
    {
        var command = new CancelManagementOrderCommand
        {
            OrderId = orderId,
            UserContext = User.GetUserContext(),
            Reason = request?.Reason
        };

        var result = await _cancelHandler.HandleAsync(command, cancellationToken);
        return StatusCode(result.StatusCode, result);
    }

    [HttpPatch("{orderId:guid}/refund-required")]
    [Authorize(Policy = "orders.manage")]
    public async Task<IActionResult> MarkRefundRequired(
        Guid orderId,
        [FromBody] ManagementOrderReasonRequest request,
        CancellationToken cancellationToken)
    {
        if (request is null || string.IsNullOrWhiteSpace(request.Reason))
        {
            return BadRequest(Application.Shared.Wrappers.ApiResult<object>.Fail("Reason is required to flag an order as refund required.", 400));
        }

        var command = new MarkOrderRefundRequiredCommand
        {
            OrderId = orderId,
            UserContext = User.GetUserContext(),
            Reason = request.Reason
        };

        var result = await _refundRequiredHandler.HandleAsync(command, cancellationToken);
        return StatusCode(result.StatusCode, result);
    }
}
