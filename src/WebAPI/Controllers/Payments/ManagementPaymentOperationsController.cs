using System.ComponentModel.DataAnnotations;
using Application.Payments.PaymentSessions.Commands;
using Application.Payments.PaymentSessions.Diagnostics;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebAPI.Authorization;

namespace WebAPI.Controllers.Payments;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/management")]
[Authorize(Policy = "payments.manage")]
public sealed class ManagementPaymentOperationsController(
    ListPaymentSessionInterventionsQueryHandler listHandler,
    ManuallyReconcilePaymentSessionCommandHandler reconcileHandler) : ControllerBase
{
    [HttpGet("payment-session-interventions")]
    public async Task<IActionResult> ListInterventions(
        [FromQuery] string? provider,
        [FromQuery] string? interventionCode,
        [FromQuery] Guid? organizationId,
        [FromQuery] Guid? storeId,
        [FromQuery] Guid? kioskId,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await listHandler.HandleAsync(
            new ListPaymentSessionInterventionsQuery(
                User.GetUserContext(),
                provider,
                interventionCode,
                organizationId,
                storeId,
                kioskId,
                pageNumber,
                pageSize),
            cancellationToken);
        return StatusCode(result.StatusCode, result);
    }

    [HttpPost("orders/{orderId:guid}/payment-transactions/{paymentTransactionId:guid}/reconcile")]
    public async Task<IActionResult> Reconcile(
        Guid orderId,
        Guid paymentTransactionId,
        [FromBody] ManualPaymentSessionReconciliationRequest request,
        CancellationToken cancellationToken)
    {
        var result = await reconcileHandler.HandleAsync(
            new ManuallyReconcilePaymentSessionCommand(
                orderId,
                paymentTransactionId,
                request.Reason,
                User.GetUserContext()),
            cancellationToken);
        return StatusCode(result.StatusCode, result);
    }
}

public sealed class ManualPaymentSessionReconciliationRequest
{
    [Required, StringLength(500)]
    public string Reason { get; init; } = string.Empty;
}
