using Application.Orders.Abstractions;
using Application.Tenants;
using Application.Tenants.Abstractions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using WebAPI.Authorization;

namespace WebAPI.SignalR.Hubs;

[Authorize]
public sealed class OrderHub : Hub
{
    private readonly IOrderStore _orderStore;
    private readonly IOrganizationAccessStateReader _organizationAccess;

    public OrderHub(IOrderStore orderStore, IOrganizationAccessStateReader organizationAccess)
    {
        _orderStore = orderStore;
        _organizationAccess = organizationAccess;
    }

    public async Task JoinOrder(Guid orderId)
    {
        var user = Context.User!.GetUserContext();
        var order = await _orderStore.GetOrderByIdAsync(orderId, Context.ConnectionAborted);
        if (order is null ||
            !order.OrganizationId.HasValue ||
            !await _organizationAccess.IsActiveAsync(order.OrganizationId.Value, Context.ConnectionAborted) ||
            !ScopeAccessRules.CanAccessScopedRow(ScopeRoleSets.OrdersView, user, order.OrganizationId, order.StoreId, order.KioskId))
        {
            throw new HubException("Access denied: you do not have access to this order.");
        }

        await Groups.AddToGroupAsync(Context.ConnectionId, $"order:{orderId}");
    }

    public async Task LeaveOrder(Guid orderId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"order:{orderId}");
    }
}
