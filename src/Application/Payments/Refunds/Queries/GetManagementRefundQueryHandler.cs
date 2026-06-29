using Application.Payments.Abstractions;
using Application.Payments.Refunds.Mapping;
using Application.Payments.Refunds.Results;
using Application.Shared.Wrappers;
using Application.Tenants;

namespace Application.Payments.Refunds.Queries;

public sealed class GetManagementRefundQueryHandler
{
    private readonly IPaymentStore _paymentStore;

    public GetManagementRefundQueryHandler(IPaymentStore paymentStore)
    {
        _paymentStore = paymentStore;
    }

    public async Task<ApiResult<RefundResult>> HandleAsync(
        GetManagementRefundQuery query,
        CancellationToken cancellationToken = default)
    {
        var refund = await _paymentStore.GetRefundByIdAsync(query.RefundId, cancellationToken);
        if (refund is null)
        {
            return ApiResult<RefundResult>.Fail("Refund not found.", 404);
        }

        var order = refund.PaymentTransaction.Order;
        if (!ScopeAccessRules.CanAccessScopedRow(ScopeRoleSets.RefundsManage,
            query.UserContext,
            order.OrganizationId,
            order.StoreId,
            order.KioskId))
        {
            return ApiResult<RefundResult>.Fail("Access denied.", 403);
        }

        return ApiResult<RefundResult>.Success(RefundResultMapper.ToResult(refund), "Refund retrieved successfully.");
    }
}
