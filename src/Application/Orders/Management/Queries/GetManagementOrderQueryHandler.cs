using Application.Orders.Abstractions;
using Application.Orders.PlaceOrder.Mapping;
using Application.Orders.PlaceOrder.Results;
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

    public async Task<ApiResult<OrderResult>> HandleAsync(
        GetManagementOrderQuery query,
        CancellationToken cancellationToken = default)
    {
        var order = await _orderStore.GetOrderByIdAsync(query.OrderId, cancellationToken);
        if (order is null)
        {
            return ApiResult<OrderResult>.Fail("Order not found.", 404);
        }

        if (!ScopeAccessRules.CanAccessScopedRow(
            query.UserContext,
            order.OrganizationId,
            order.StoreId,
            order.KioskId))
        {
            return ApiResult<OrderResult>.Fail("Access denied.", 403);
        }

        return ApiResult<OrderResult>.Success(OrderResultMapper.ToResult(order), "Order retrieved successfully.");
    }
}
