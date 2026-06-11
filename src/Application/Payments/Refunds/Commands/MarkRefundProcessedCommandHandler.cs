using Application.Payments.Abstractions;
using Application.Payments.Refunds.Results;
using Application.Shared.Wrappers;
using Application.Tenants;
using Domain.Orders.Enums;
using Domain.Payments.Enums;

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

            // Parse refund method from serialized metadata in Reason
            var parsed = Mapping.RefundResultMapper.ParseReason(refund.Reason);
            var method = parsed.Method;

            var previousStatus = order.Status;
            var newStatus = OrderStatus.Refunded;

            if (string.Equals(method, "Voucher", StringComparison.OrdinalIgnoreCase))
            {
                newStatus = OrderStatus.Compensated;
                order.MarkCompensated();
                // do not set order.PaymentStatus = PaymentStatus.Refunded because the payment was not reversed
            }
            else
            {
                newStatus = OrderStatus.Refunded;
                order.MarkRefunded();
                var moneyWasRefunded = command.MoneyWasRefunded ?? true;
                if (moneyWasRefunded)
                {
                    order.PaymentStatus = PaymentStatus.Refunded;
                    transaction.Status = PaymentTransactionStatus.Refunded;
                }
            }

            order.UpdatedAt = now;

            // Record OrderStatusHistory
            var history = new Domain.Orders.Entities.OrderStatusHistory
            {
                Id = Guid.NewGuid(),
                OrderId = order.Id,
                FromStatus = previousStatus,
                ToStatus = newStatus,
                ChangedAt = now,
                Reason = $"Refund processed via {method}.",
                ChangedByAccountId = command.UserContext.AccountId
            };
            await _paymentStore.AddOrderStatusHistoryAsync(history, ct);

            await _paymentStore.SaveChangesAsync(ct);

            return ApiResult<RefundResult>.Success(
                Mapping.RefundResultMapper.ToResult(refund),
                "Refund marked as processed successfully.");
        }, cancellationToken);
    }
}
