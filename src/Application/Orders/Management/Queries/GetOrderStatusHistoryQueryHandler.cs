using Application.Orders.Abstractions;
using Application.Orders.Management.Mapping;
using Application.Orders.Management.Results;
using Application.Shared.Wrappers;
using Application.Tenants;

namespace Application.Orders.Management.Queries;

public sealed class GetOrderStatusHistoryQueryHandler
{
    private readonly IOrderStore _orderStore;

    public GetOrderStatusHistoryQueryHandler(IOrderStore orderStore)
    {
        _orderStore = orderStore;
    }

    public async Task<PagedResult<OrderStatusHistoryResult>> HandleAsync(
        GetOrderStatusHistoryQuery query,
        CancellationToken cancellationToken = default)
    {
        var pageNumber = Math.Max(query.PageNumber, 1);
        var pageSize = Math.Clamp(query.PageSize, 1, 100);

        var order = await _orderStore.GetOrderByIdAsync(query.OrderId, cancellationToken);
        if (order is null)
        {
            return PagedResult<OrderStatusHistoryResult>.Fail("Order not found.", 404, pageNumber, pageSize);
        }

        if (!ScopeAccessRules.CanAccessScopedRow(ScopeRoleSets.OrdersView,
            query.UserContext,
            order.OrganizationId,
            order.StoreId,
            order.KioskId))
        {
            return PagedResult<OrderStatusHistoryResult>.Forbidden("Access denied.", pageNumber, pageSize);
        }

        var totalCount = await _orderStore.CountOrderStatusHistoryAsync(query.OrderId, cancellationToken);
        var history = await _orderStore.ListOrderStatusHistoryAsync(
            query.OrderId,
            pageNumber,
            pageSize,
            cancellationToken);

        return PagedResult<OrderStatusHistoryResult>.Success(
            history.Select(OrderStatusHistoryResultMapper.ToResult),
            totalCount,
            pageNumber,
            pageSize,
            "Order status history retrieved successfully.");
    }
}
