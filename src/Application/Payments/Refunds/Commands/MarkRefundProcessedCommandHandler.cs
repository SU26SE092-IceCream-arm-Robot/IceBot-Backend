using Application.Abstractions.Realtime;
using Application.Abstractions.Realtime.Events;
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
    private readonly IRealtimeNotificationPublisher _publisher;

    public MarkRefundProcessedCommandHandler(IPaymentStore paymentStore, IRealtimeNotificationPublisher publisher)
    {
        _paymentStore = paymentStore;
        _publisher = publisher;
    }

    public async Task<ApiResult<RefundResult>> HandleAsync(
        MarkRefundProcessedCommand command,
        CancellationToken cancellationToken = default)
    {
        OrderStatus originalOrderStatus = OrderStatus.Draft;
        OrderStatus newOrderStatus = OrderStatus.Draft;
        PaymentTransactionStatus originalTxStatus = PaymentTransactionStatus.Pending;
        PaymentTransactionStatus newTxStatus = PaymentTransactionStatus.Pending;
        bool statusChanged = false;
        bool txStatusChanged = false;

        Guid? orgId = null;
        Guid? storeId = null;
        Guid kioskId = Guid.Empty;
        Guid orderId = Guid.Empty;
        Guid transactionId = Guid.Empty;
        string orderNumber = "";
        string provider = "";
        string customerStatus = "";
        string customerStatusMessage = "";
        string orderPaymentStatus = "";
        bool canRetryPayment = false;
        bool requiresStaffSupport = false;

        var result = await _paymentStore.ExecuteInTransactionAsync(async ct =>
        {
            await _paymentStore.AcquireRefundLockAsync(command.RefundId, ct);
            var refund = await _paymentStore.GetRefundByIdAsync(command.RefundId, ct);
            if (refund is null)
            {
                return ApiResult<RefundResult>.Fail("Refund not found.", 404);
            }

            var transaction = refund.PaymentTransaction;
            var order = transaction.Order;

            await _paymentStore.AcquireOrderWorkflowLockAsync(order.Id, ct);
            await _paymentStore.ReloadOrderAsync(order, ct);

            originalOrderStatus = order.Status;
            originalTxStatus = transaction.Status;
            orgId = order.OrganizationId;
            storeId = order.StoreId;
            kioskId = order.KioskId;
            orderId = order.Id;
            transactionId = transaction.Id;
            orderNumber = order.OrderNumber;
            provider = transaction.Provider;

            if (!ScopeAccessRules.CanAccessScopedRow(ScopeRoleSets.RefundsProcess,
                command.UserContext,
                order.OrganizationId,
                order.StoreId,
                order.KioskId))
            {
                return ApiResult<RefundResult>.Fail("Access denied.", 403);
            }

            var now = DateTimeOffset.UtcNow;

            var method = refund.CompensationMethod;

            if (refund.Status == RefundStatus.Processed)
            {
                return ApiResult<RefundResult>.Success(
                    Mapping.RefundResultMapper.ToResult(refund),
                    "Refund was already marked as processed.");
            }

            if (method != RefundCompensationMethod.Voucher &&
                command.MoneyWasRefunded is not true)
            {
                return ApiResult<RefundResult>.Fail(
                    "MoneyWasRefunded must be true before completing a full money refund.",
                    400);
            }

            refund.MarkProcessed(command.ProviderRefundId, now);

            var previousStatus = order.Status;
            var isDuplicatePayment = transaction.SettlementDisposition ==
                PaymentSettlementDisposition.DuplicateRefundRequired;
            var moneyWasRefunded = method == RefundCompensationMethod.FullMoneyRefund;

            if (isDuplicatePayment)
            {
                transaction.ResolveDuplicatePayment(moneyWasRefunded);
                var hasOtherDuplicatePayments = await _paymentStore.HasOtherUnresolvedDuplicatePaymentsAsync(
                    order.Id,
                    transaction.Id,
                    ct);
                if (!hasOtherDuplicatePayments)
                {
                    order.ResolveDuplicatePaymentIntervention();
                }
            }
            else
            {
                if (!moneyWasRefunded)
                {
                    order.MarkCompensated();
                    // A voucher compensates the customer but does not reverse the payment.
                }
                else
                {
                    order.MarkRefunded();
                    order.MarkPaymentRefunded();
                    transaction.Status = PaymentTransactionStatus.Refunded;
                }
            }

            order.UpdatedAt = now;

            newOrderStatus = order.Status;
            newTxStatus = transaction.Status;
            orderPaymentStatus = order.PaymentStatus.ToString();
            statusChanged = newOrderStatus != originalOrderStatus;
            txStatusChanged = newTxStatus != originalTxStatus;
            var customerStatusInfo = Application.Orders.Support.OrderStatusProjector.ProjectFromOrder(order);
            customerStatus = customerStatusInfo.CustomerStatus;
            customerStatusMessage = customerStatusInfo.CustomerStatusMessage;
            canRetryPayment = customerStatusInfo.CanRetryPayment;
            requiresStaffSupport = customerStatusInfo.RequiresStaffSupport;

            if (order.Status != previousStatus)
            {
                var history = new Domain.Orders.Entities.OrderStatusHistory
                {
                    Id = Guid.NewGuid(),
                    OrderId = order.Id,
                    FromStatus = previousStatus,
                    ToStatus = order.Status,
                    ChangedAt = now,
                    Reason = isDuplicatePayment
                        ? $"Duplicate payment intervention resolved via {method}."
                        : $"Refund processed via {method}.",
                    ChangedByAccountId = command.UserContext.AccountId
                };
                await _paymentStore.AddOrderStatusHistoryAsync(history, ct);
            }

            await _paymentStore.SaveChangesAsync(ct);

            return ApiResult<RefundResult>.Success(
                Mapping.RefundResultMapper.ToResult(refund),
                "Refund marked as processed successfully.");
        }, cancellationToken);

        if (result.Succeeded && result.Data is not null)
        {
            if (txStatusChanged)
            {
                await _publisher.PublishPaymentStatusChangedAsync(new PaymentStatusChangedEvent
                {
                    OrderId = orderId,
                    PaymentTransactionId = transactionId,
                    OldStatus = originalTxStatus.ToString(),
                    NewStatus = newTxStatus.ToString(),
                    Provider = provider,
                    UpdatedAt = DateTimeOffset.UtcNow,
                    Version = 1,
                    OrganizationId = orgId,
                    StoreId = storeId
                }, cancellationToken);
            }

            if (statusChanged)
            {
                await _publisher.PublishOrderStatusChangedAsync(new OrderStatusChangedEvent
                {
                    OrderId = orderId,
                    OrderNumber = orderNumber,
                    KioskId = kioskId,
                    OrganizationId = orgId,
                    StoreId = storeId,
                    OldStatus = originalOrderStatus.ToString(),
                    NewStatus = newOrderStatus.ToString(),
                    PaymentStatus = orderPaymentStatus,
                    CustomerStatus = customerStatus,
                    CustomerStatusMessage = customerStatusMessage,
                    CanRetryPayment = canRetryPayment,
                    RequiresStaffSupport = requiresStaffSupport,
                    UpdatedAt = DateTimeOffset.UtcNow,
                    Version = 1
                }, cancellationToken);
            }
        }

        return result;
    }
}
