using Application.Payments.Refunds.Commands;
using Application.Payments.Refunds.Queries;
using Application.Payments.Refunds.Requests;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebAPI.Authorization;

namespace WebAPI.Controllers.Payments;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/management")]
[Authorize(Policy = "refunds.manage")]
public sealed class ManagementRefundsController : ControllerBase
{
    private readonly ListManagementRefundsQueryHandler _listHandler;
    private readonly GetManagementRefundQueryHandler _getHandler;
    private readonly RequestRefundCommandHandler _requestHandler;
    private readonly MarkRefundProcessedCommandHandler _processedHandler;
    private readonly RejectRefundCommandHandler _rejectHandler;
    private readonly CancelRefundCommandHandler _cancelHandler;

    public ManagementRefundsController(
        ListManagementRefundsQueryHandler listHandler,
        GetManagementRefundQueryHandler getHandler,
        RequestRefundCommandHandler requestHandler,
        MarkRefundProcessedCommandHandler processedHandler,
        RejectRefundCommandHandler rejectHandler,
        CancelRefundCommandHandler cancelHandler)
    {
        _listHandler = listHandler;
        _getHandler = getHandler;
        _requestHandler = requestHandler;
        _processedHandler = processedHandler;
        _rejectHandler = rejectHandler;
        _cancelHandler = cancelHandler;
    }

    [HttpGet("refunds")]
    public async Task<IActionResult> ListRefunds(
        [FromQuery] string? search,
        [FromQuery] Domain.Payments.Enums.RefundStatus? status,
        [FromQuery] Guid? organizationId,
        [FromQuery] Guid? storeId,
        [FromQuery] Guid? kioskId,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var query = new ListManagementRefundsQuery
        {
            UserContext = User.GetUserContext(),
            Search = search,
            Status = status,
            OrganizationId = organizationId,
            StoreId = storeId,
            KioskId = kioskId,
            PageNumber = pageNumber,
            PageSize = pageSize
        };

        var result = await _listHandler.HandleAsync(query, cancellationToken);
        return StatusCode(result.StatusCode, result);
    }

    [HttpGet("refunds/{refundId:guid}")]
    public async Task<IActionResult> GetRefund(Guid refundId, CancellationToken cancellationToken)
    {
        var query = new GetManagementRefundQuery
        {
            RefundId = refundId,
            UserContext = User.GetUserContext()
        };

        var result = await _getHandler.HandleAsync(query, cancellationToken);
        return StatusCode(result.StatusCode, result);
    }

    [HttpPost("orders/{orderId:guid}/refunds")]
    public async Task<IActionResult> RequestRefund(
        Guid orderId,
        [FromBody] RequestRefundRequest request,
        [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey,
        CancellationToken cancellationToken)
    {
        if (request is null || string.IsNullOrWhiteSpace(request.Reason))
        {
            return BadRequest(Application.Shared.Wrappers.ApiResult<object>.Fail("Reason is required to request a refund.", 400));
        }

        if (string.IsNullOrWhiteSpace(request.RefundMethod))
        {
            return BadRequest(Application.Shared.Wrappers.ApiResult<object>.Fail("RefundMethod is required.", 400));
        }

        var command = new RequestRefundCommand
        {
            OrderId = orderId,
            UserContext = User.GetUserContext(),
            RefundMethod = request.RefundMethod,
            Reason = request.Reason,
            VoucherCode = request.VoucherCode,
            VoucherValue = request.VoucherValue,
            Note = request.Note,
            IdempotencyKey = idempotencyKey
        };

        var result = await _requestHandler.HandleAsync(command, cancellationToken);
        return StatusCode(result.StatusCode, result);
    }

    [HttpPatch("refunds/{refundId:guid}/mark-processed")]
    public async Task<IActionResult> MarkProcessed(
        Guid refundId,
        [FromBody] MarkRefundProcessedRequest? request,
        CancellationToken cancellationToken)
    {
        var command = new MarkRefundProcessedCommand
        {
            RefundId = refundId,
            UserContext = User.GetUserContext(),
            ProviderRefundId = request?.ProviderRefundId,
            MoneyWasRefunded = request?.MoneyWasRefunded
        };

        var result = await _processedHandler.HandleAsync(command, cancellationToken);
        return StatusCode(result.StatusCode, result);
    }

    [HttpPatch("refunds/{refundId:guid}/reject")]
    public async Task<IActionResult> RejectRefund(
        Guid refundId,
        [FromBody] RefundReasonRequest request,
        CancellationToken cancellationToken)
    {
        if (request is null || string.IsNullOrWhiteSpace(request.Reason))
        {
            return BadRequest(Application.Shared.Wrappers.ApiResult<object>.Fail("Reason is required to reject a refund.", 400));
        }

        var command = new RejectRefundCommand
        {
            RefundId = refundId,
            UserContext = User.GetUserContext(),
            Reason = request.Reason
        };

        var result = await _rejectHandler.HandleAsync(command, cancellationToken);
        return StatusCode(result.StatusCode, result);
    }

    [HttpPatch("refunds/{refundId:guid}/cancel")]
    public async Task<IActionResult> CancelRefund(
        Guid refundId,
        [FromBody] RefundReasonRequest? request,
        CancellationToken cancellationToken)
    {
        var command = new CancelRefundCommand
        {
            RefundId = refundId,
            UserContext = User.GetUserContext(),
            Reason = request?.Reason
        };

        var result = await _cancelHandler.HandleAsync(command, cancellationToken);
        return StatusCode(result.StatusCode, result);
    }
}
