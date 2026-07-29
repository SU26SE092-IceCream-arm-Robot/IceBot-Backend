using System.Text.Json;
using Application.Operations.OperationLogs.Abstractions;
using Application.Payments.Abstractions;
using Application.Shared.Wrappers;
using Application.Tenants;
using Domain.Common.Enums;
using Domain.Operations.Entities;
using Domain.Payments.Enums;
using Application.Payments.PaymentSessions.Support;

namespace Application.Payments.PaymentSessions.Commands;

public sealed class ManuallyReconcilePaymentSessionCommandHandler(
    IPaymentStore paymentStore,
    IOperationLogStore operationLogStore,
    ReconcilePendingPaymentSessionCommandHandler reconcileHandler)
{
    public async Task<ApiResult<ManualPaymentSessionReconciliationResult>> HandleAsync(
        ManuallyReconcilePaymentSessionCommand command,
        CancellationToken cancellationToken = default)
    {
        var reason = command.Reason?.Trim();
        if (string.IsNullOrWhiteSpace(reason) || reason.Length > 500)
        {
            return ApiResult<ManualPaymentSessionReconciliationResult>.Fail(
                "Reason is required and must be at most 500 characters.",
                400);
        }

        var payment = await paymentStore.GetPaymentTransactionByIdAsync(
            command.PaymentTransactionId,
            cancellationToken);
        if (payment is null || payment.OrderId != command.OrderId)
        {
            return ApiResult<ManualPaymentSessionReconciliationResult>.Fail(
                "Payment transaction not found for the order.",
                404);
        }

        if (!ScopeAccessRules.CanAccessScopedRow(
                ScopeRoleSets.PaymentsManage,
                command.UserContext,
                payment.Order.OrganizationId,
                payment.Order.StoreId,
                payment.Order.KioskId))
        {
            return ApiResult<ManualPaymentSessionReconciliationResult>.Fail("Access denied.", 403);
        }

        var requestedAt = DateTimeOffset.UtcNow;
        if (!PaymentSessionInterventionPolicy.CanReconcile(payment, requestedAt))
        {
            return ApiResult<ManualPaymentSessionReconciliationResult>.Fail(
                "Payment session is not eligible for reconciliation.",
                409);
        }

        await WriteAuditAsync(
            payment,
            command.UserContext.AccountId,
            "PaymentSessionReconciliationRequested",
            requestedAt,
            new { paymentTransactionId = payment.Id, reason },
            cancellationToken);

        var outcome = await reconcileHandler.HandleAsync(
            new ReconcilePendingPaymentSessionCommand(
                payment.Id,
                requestedAt,
                requestedAt.AddSeconds(30)),
            cancellationToken);

        var refreshed = await paymentStore.GetPaymentTransactionByIdAsync(payment.Id, cancellationToken)
            ?? payment;
        await WriteAuditAsync(
            refreshed,
            command.UserContext.AccountId,
            "PaymentSessionReconciliationCompleted",
            DateTimeOffset.UtcNow,
            new
            {
                paymentTransactionId = refreshed.Id,
                reason,
                outcome = outcome.ToString(),
                status = refreshed.Status.ToString(),
                interventionCode = refreshed.LastErrorCode
            },
            cancellationToken);

        return ApiResult<ManualPaymentSessionReconciliationResult>.Success(
            new ManualPaymentSessionReconciliationResult(
                refreshed.Id,
                refreshed.OrderId,
                outcome,
                refreshed.Status.ToString(),
                refreshed.LastErrorCode,
                refreshed.RetryCount,
                refreshed.NextRetryAt),
            "Payment-session reconciliation completed.");
    }

    private async Task WriteAuditAsync(
        Domain.Payments.Entities.PaymentTransaction payment,
        Guid accountId,
        string action,
        DateTimeOffset occurredAt,
        object payload,
        CancellationToken cancellationToken)
    {
        await operationLogStore.AddAsync(new OperationLog
        {
            AccountId = accountId,
            KioskId = payment.Order.KioskId,
            OrderId = payment.OrderId,
            Action = action,
            Category = "Payment",
            Severity = SeverityLevel.Info,
            Message = action == "PaymentSessionReconciliationRequested"
                ? "Manual payment-session reconciliation requested."
                : "Manual payment-session reconciliation completed.",
            PayloadJson = JsonSerializer.Serialize(payload),
            OccurredAt = occurredAt,
            OriginNodeId = Guid.Empty,
            Version = 1,
            SyncedAt = occurredAt
        }, cancellationToken);
        await operationLogStore.SaveChangesAsync(cancellationToken);
    }
}
