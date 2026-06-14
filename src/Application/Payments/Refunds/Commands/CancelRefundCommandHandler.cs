using Application.Payments.Abstractions;
using Application.Payments.Refunds.Results;
using Application.Shared.Wrappers;
using Application.Tenants;

namespace Application.Payments.Refunds.Commands;

public sealed class CancelRefundCommandHandler
{
    private readonly IPaymentStore _paymentStore;
    private readonly IRealtimeNotificationPublisher _publisher;

    public CancelRefundCommandHandler(IPaymentStore paymentStore, IRealtimeNotificationPublisher publisher)
    {
        _paymentStore = paymentStore;
        _publisher = publisher;
    }

    public async Task<ApiResult<RefundResult>> HandleAsync(
        CancelRefundCommand command,
        CancellationToken cancellationToken = default)
    {
        Guid? orgId = null;
        Guid? storeId = null;

        var result = await _paymentStore.ExecuteInTransactionAsync(async ct =>
        {
            var refund = await _paymentStore.GetRefundByIdAsync(command.RefundId, ct);
            if (refund is null)
            {
                return ApiResult<RefundResult>.Fail("Refund not found.", 404);
            }

            var order = refund.PaymentTransaction.Order;

            orgId = order.OrganizationId;
            storeId = order.StoreId;

            if (!ScopeAccessRules.CanAccessScopedRow(
                command.UserContext,
                order.OrganizationId,
                order.StoreId,
                order.KioskId))
            {
                return ApiResult<RefundResult>.Fail("Access denied.", 403);
            }

            var now = DateTimeOffset.UtcNow;

            refund.Cancel();
            refund.LastErrorMessage = command.Reason ?? "Cancelled by staff";
            refund.UpdatedAt = now;

            await _paymentStore.SaveChangesAsync(ct);

            return ApiResult<RefundResult>.Success(
                Mapping.RefundResultMapper.ToResult(refund),
                "Refund cancelled successfully.");
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
