using Application.Orders.Abstractions;
using Application.Orders.Management.Mapping;
using Application.Orders.Management.Results;
using Application.Shared.Wrappers;
using Application.Tenants;

namespace Application.Orders.Management.Queries;

public sealed class ListManagementOrdersQueryHandler
{
    private readonly IOrderStore _orderStore;

    public ListManagementOrdersQueryHandler(IOrderStore orderStore)
    {
        _orderStore = orderStore;
    }

    public async Task<PagedResult<ManagementOrderListItemResult>> HandleAsync(
        ListManagementOrdersQuery query,
        CancellationToken cancellationToken = default)
    {
        var pageNumber = Math.Max(query.PageNumber, 1);
        var pageSize = Math.Clamp(query.PageSize, 1, 100);
        var scope = ScopeAccessRules.GetEffectiveScope(ScopeRoleSets.OrdersView, query.UserContext);

        var totalCount = await _orderStore.CountOrdersAsync(
            query.Search,
            query.Status,
            query.PaymentStatus,
            query.OrganizationId,
            query.StoreId,
            query.KioskId,
            query.UserContext.IsSystemAdmin,
            scope.OrganizationIds,
            scope.StoreIds,
            scope.KioskIds,
            cancellationToken);

        var orders = await _orderStore.ListOrdersAsync(
            query.Search,
            query.Status,
            query.PaymentStatus,
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

        return PagedResult<ManagementOrderListItemResult>.Success(
            orders.Select(ManagementOrderResultMapper.ToListItem),
            totalCount,
            pageNumber,
            pageSize,
            "Orders retrieved successfully.");
    }
}
