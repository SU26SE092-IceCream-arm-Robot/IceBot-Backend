using Application.Abstractions.Realtime;
using Application.Abstractions.Realtime.Events;
using Application.Payments.Abstractions;
using Application.Payments.Refunds.Mapping;
using Application.Payments.Refunds.Results;
using Application.Shared.Wrappers;
using Application.Tenants;

namespace Application.Payments.Refunds.Commands;

public sealed class RejectRefundCommandHandler
{
    private readonly IPaymentStore _paymentStore;
    private readonly IRealtimeNotificationPublisher _publisher;

    public RejectRefundCommandHandler(IPaymentStore paymentStore, IRealtimeNotificationPublisher publisher)
    {
        _paymentStore = paymentStore;
        _publisher = publisher;
    }

    public async Task<ApiResult<RefundResult>> HandleAsync(
        RejectRefundCommand command,
        CancellationToken cancellationToken = default)
    {
        var reason = command.Reason?.Trim();
        if (string.IsNullOrWhiteSpace(reason))
        {
            return ApiResult<RefundResult>.Fail("Reason is required to reject a refund.", 400);
        }

        Guid? orgId = null;
        Guid? storeId = null;

        var result = await _paymentStore.ExecuteInTransactionAsync(async ct =>
        {
            await _paymentStore.AcquireRefundLockAsync(command.RefundId, ct);
            var refund = await _paymentStore.GetRefundByIdAsync(command.RefundId, ct);
            if (refund is null)
            {
                return ApiResult<RefundResult>.Fail("Refund not found.", 404);
            }

            var order = refund.PaymentTransaction.Order;

            orgId = order.OrganizationId;
            storeId = order.StoreId;

            if (!ScopeAccessRules.CanAccessScopedRow(ScopeRoleSets.RefundsProcess,
                command.UserContext,
                order.OrganizationId,
                order.StoreId,
                order.KioskId))
            {
                return ApiResult<RefundResult>.Fail("Access denied.", 403);
            }

            var now = DateTimeOffset.UtcNow;

            refund.Reject(now, reason);
            refund.UpdatedAt = now;

            await _paymentStore.SaveChangesAsync(ct);

            return ApiResult<RefundResult>.Success(
                RefundResultMapper.ToResult(refund),
                "Refund rejected successfully.");
        }, cancellationToken);

        if (result.Succeeded && result.Data is not null)
        {
            await _publisher.PublishDashboardInvalidatedAsync(new DashboardInvalidatedEvent
            {
                Scope = "Organization",
                OrganizationId = orgId,
                StoreId = storeId,
                Reason = "RefundChanged",
                UpdatedAt = DateTimeOffset.UtcNow,
            }, cancellationToken);
        }

        return result;
    }
}
