using Application.Orders.Management.Abstractions;
using Application.Orders.Management.Results;
using Application.Shared.Wrappers;
using Application.Tenants;

namespace Application.Orders.Management.Queries;

public sealed class GetOrderItemStatusHistoryQueryHandler(IOrderFulfillmentReadStore fulfillment)
{
    public async Task<PagedResult<OrderItemStatusHistoryResult>> HandleAsync(
        GetOrderItemStatusHistoryQuery query,
        CancellationToken cancellationToken = default)
    {
        var pageNumber = Math.Max(query.PageNumber, 1);
        var pageSize = Math.Clamp(query.PageSize, 1, 100);
        var owner = await fulfillment.GetItemOwnerAsync(query.OrderItemId, cancellationToken);
        if (owner is null)
            return PagedResult<OrderItemStatusHistoryResult>.Fail(
                "Order item not found.", 404, pageNumber, pageSize);
        if (!ScopeAccessRules.CanAccessScopedRow(
                ScopeRoleSets.OrdersView, query.UserContext,
                owner.OrganizationId, owner.StoreId, owner.KioskId))
            return PagedResult<OrderItemStatusHistoryResult>.Fail(
                "Order item not found.", 404, pageNumber, pageSize);

        var totalCount = await fulfillment.CountItemStatusHistoryAsync(query.OrderItemId, cancellationToken);
        var history = await fulfillment.ListItemStatusHistoryAsync(
            query.OrderItemId, pageNumber, pageSize, cancellationToken);
        return PagedResult<OrderItemStatusHistoryResult>.Success(history.Select(item =>
            new OrderItemStatusHistoryResult
            {
                Id = item.Id,
                OrderItemId = item.OrderItemId,
                ChangedByAccountId = item.ChangedByAccountId,
                ChangedByName = item.ChangedByName,
                ChangedByEmail = item.ChangedByEmail,
                FromStatus = item.FromStatus,
                ToStatus = item.ToStatus,
                Reason = item.Reason,
                ChangedAt = item.ChangedAt
            }), totalCount, pageNumber, pageSize);
    }
}
