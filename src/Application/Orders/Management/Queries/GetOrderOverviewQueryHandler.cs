using Application.Orders.Abstractions;
using Application.Orders.Management.Results;
using Application.Shared.Wrappers;
using Application.Tenants;

namespace Application.Orders.Management.Queries;

public sealed class GetOrderOverviewQueryHandler
{
    private readonly IOrderStore _orderStore;

    public GetOrderOverviewQueryHandler(IOrderStore orderStore)
    {
        _orderStore = orderStore;
    }

    public async Task<ApiResult<OrderOverviewResult>> HandleAsync(
        GetOrderOverviewQuery query,
        CancellationToken cancellationToken = default)
    {
        var userContext = query.UserContext;
        var take = Math.Clamp(query.Take, 1, 50);
        var scope = ScopeAccessRules.GetEffectiveScope(ScopeRoleSets.OrdersView, userContext);

        var overview = await _orderStore.GetOrderOverviewAsync(
            query.From,
            query.To,
            query.Status,
            query.KioskId,
            take,
            userContext.IsSystemAdmin,
            scope.OrganizationIds,
            scope.StoreIds,
            scope.KioskIds,
            cancellationToken);

        return ApiResult<OrderOverviewResult>.Success(overview, "Order overview retrieved successfully.");
    }
}
