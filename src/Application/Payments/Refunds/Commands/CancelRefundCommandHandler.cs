using Application.Payments.Abstractions;
using Application.Payments.Refunds.Results;
using Application.Shared.Wrappers;
using Application.Tenants;

namespace Application.Payments.Refunds.Commands;

public sealed class CancelRefundCommandHandler
{
    private readonly IPaymentStore _paymentStore;

    public CancelRefundCommandHandler(IPaymentStore paymentStore)
    {
        _paymentStore = paymentStore;
    }

    public async Task<ApiResult<RefundResult>> HandleAsync(
        CancelRefundCommand command,
        CancellationToken cancellationToken = default)
    {
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

            refund.Cancel();
            refund.LastErrorMessage = command.Reason ?? "Cancelled by staff";
            refund.UpdatedAt = now;

            await _paymentStore.SaveChangesAsync(ct);

            return ApiResult<RefundResult>.Success(
                Mapping.RefundResultMapper.ToResult(refund),
                "Refund cancelled successfully.");
        }, cancellationToken);
    }
}
