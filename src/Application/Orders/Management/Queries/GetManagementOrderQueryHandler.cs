using Application.Orders.Abstractions;
using Application.Orders.Management.Mapping;
using Application.Orders.Management.Results;
using Application.Shared.Wrappers;
using Application.Tenants;

namespace Application.Orders.Management.Queries;

public sealed class GetManagementOrderQueryHandler
{
    private readonly IOrderStore _orderStore;

    public GetManagementOrderQueryHandler(IOrderStore orderStore)
    {
        _orderStore = orderStore;
    }

    public async Task<ApiResult<ManagementOrderDetailResult>> HandleAsync(
        GetManagementOrderQuery query,
        CancellationToken cancellationToken = default)
    {
        var order = await _orderStore.GetOrderByIdAsync(query.OrderId, cancellationToken);
        if (order is null)
        {
            return ApiResult<ManagementOrderDetailResult>.Fail("Order not found.", 404);
        }

        if (!ScopeAccessRules.CanAccessScopedRow(ScopeRoleSets.OrdersView,
            query.UserContext,
            order.OrganizationId,
            order.StoreId,
            order.KioskId))
        {
            return ApiResult<ManagementOrderDetailResult>.Fail("Access denied.", 403);
        }

        return ApiResult<ManagementOrderDetailResult>.Success(
            ManagementOrderResultMapper.ToDetail(order),
            "Order retrieved successfully.");
    }
}
