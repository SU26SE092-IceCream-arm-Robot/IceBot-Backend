using Application.Orders.Management.Commands;
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
    private readonly CancelManagementOrderCommandHandler _cancelHandler;
    private readonly MarkOrderRefundRequiredCommandHandler _refundRequiredHandler;
    private readonly RedispatchOrderExecutionCommandHandler _redispatchHandler;

    public ManagementOrdersController(
        CancelManagementOrderCommandHandler cancelHandler,
        MarkOrderRefundRequiredCommandHandler refundRequiredHandler,
        RedispatchOrderExecutionCommandHandler redispatchHandler)
    {
        _cancelHandler = cancelHandler;
        _refundRequiredHandler = refundRequiredHandler;
        _redispatchHandler = redispatchHandler;
    }

    [HttpPost("{orderId:guid}/execution-attempts")]
    [Authorize(Policy = "orders.manage")]
    public async Task<IActionResult> RedispatchOrderExecution(
        Guid orderId,
        [FromBody] ManagementOrderReasonRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _redispatchHandler.HandleAsync(new RedispatchOrderExecutionCommand
        {
            OrderId = orderId,
            UserContext = User.GetUserContext(),
            Reason = request.Reason
        }, cancellationToken);
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
