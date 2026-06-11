using Application.Payments.Abstractions;
using Application.Payments.Refunds.Mapping;
using Application.Payments.Refunds.Results;
using Application.Shared.Wrappers;
using Application.Tenants;
using Domain.Orders.Enums;
using Domain.Payments.Enums;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Application.Payments.Refunds.Commands;

public sealed class MarkRefundProcessedCommandHandler
{
    private readonly IPaymentStore _paymentStore;

    public MarkRefundProcessedCommandHandler(IPaymentStore paymentStore)
    {
        _paymentStore = paymentStore;
    }

    public async Task<ApiResult<RefundResult>> HandleAsync(
        MarkRefundProcessedCommand command,
        CancellationToken cancellationToken = default)
    {
        return await _paymentStore.ExecuteInTransactionAsync(async ct =>
        {
            var refund = await _paymentStore.GetRefundByIdAsync(command.RefundId, ct);
            if (refund is null)
            {
                return ApiResult<RefundResult>.Fail("Refund not found.", 404);
            }

            var transaction = refund.PaymentTransaction;
            var order = transaction.Order;

            if (!ScopeAccessRules.CanAccessScopedRow(
                command.UserContext,
                order.OrganizationId,
                order.StoreId,
                order.KioskId))
            {
                return ApiResult<RefundResult>.Fail("Access denied.", 403);
            }

            var now = DateTimeOffset.UtcNow;
            
            // Mark refund as processed
            refund.MarkProcessed(command.ProviderRefundId, now);

            // Update parent payment and order statuses
            transaction.Status = PaymentTransactionStatus.Refunded;
            order.PaymentStatus = PaymentStatus.Refunded;
            order.UpdatedAt = now;

            await _paymentStore.SaveChangesAsync(ct);

            return ApiResult<RefundResult>.Success(
                RefundResultMapper.ToResult(refund),
                "Refund marked as processed successfully.");
        }, cancellationToken);
    }
}
