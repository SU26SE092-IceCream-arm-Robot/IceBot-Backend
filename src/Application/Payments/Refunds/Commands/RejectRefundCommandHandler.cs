using Application.Payments.Abstractions;
using Application.Payments.Refunds.Mapping;
using Application.Payments.Refunds.Results;
using Application.Shared.Wrappers;
using Application.Tenants;

namespace Application.Payments.Refunds.Commands;

public sealed class RejectRefundCommandHandler
{
    private readonly IPaymentStore _paymentStore;

    public RejectRefundCommandHandler(IPaymentStore paymentStore)
    {
        _paymentStore = paymentStore;
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

        return await _paymentStore.ExecuteInTransactionAsync(async ct =>
        {
            var refund = await _paymentStore.GetRefundByIdAsync(command.RefundId, ct);
            if (refund is null)
            {
                return ApiResult<RefundResult>.Fail("Refund not found.", 404);
            }

            var order = refund.PaymentTransaction.Order;

            if (!ScopeAccessRules.CanAccessScopedRow(
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
    }
}
