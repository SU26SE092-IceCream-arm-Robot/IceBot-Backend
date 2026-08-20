using Application.Payments.Abstractions;
using Application.Payments.Refunds.Mapping;
using Application.Payments.Refunds.Results;
using Application.Shared.Wrappers;
using Application.Tenants;

namespace Application.Payments.Refunds.Queries;

public sealed class ListManagementRefundsQueryHandler
{
    private readonly IPaymentStore _paymentStore;

    public ListManagementRefundsQueryHandler(IPaymentStore paymentStore)
    {
        _paymentStore = paymentStore;
    }

    public async Task<PagedResult<RefundResult>> HandleAsync(
        ListManagementRefundsQuery query,
        CancellationToken cancellationToken = default)
    {
        var pageNumber = Math.Max(query.PageNumber, 1);
        var pageSize = Math.Clamp(query.PageSize, 1, 100);
        var scope = ScopeAccessRules.GetEffectiveScope(ScopeRoleSets.RefundsView, query.UserContext);

        var totalCount = await _paymentStore.CountRefundsAsync(
            query.Search,
            query.Status,
            query.OrganizationId,
            query.StoreId,
            query.KioskId,
            query.UserContext.IsSystemAdmin,
            scope.OrganizationIds,
            scope.StoreIds,
            scope.KioskIds,
            cancellationToken);

        var refunds = await _paymentStore.ListRefundsAsync(
            query.Search,
            query.Status,
            query.OrganizationId,
            query.StoreId,
            query.KioskId,
            query.UserContext.IsSystemAdmin,
            scope.OrganizationIds,
            scope.StoreIds,
            scope.KioskIds,
            pageNumber,
            pageSize,
            cancellationToken);

        return PagedResult<RefundResult>.Success(
            refunds.Select(RefundResultMapper.ToResult),
            totalCount,
            pageNumber,
            pageSize,
            "Refunds retrieved successfully.");
    }
}
