using Application.Abstractions.Realtime;
using Application.Abstractions.Realtime.Events;
using Application.EdgeIntegration.CommandDelivery.Commands;
using Application.EdgeIntegration.Dispatch.Commands;
using Application.EdgeIntegration.Reports.Commands;
using Application.EdgeIntegration.Timeouts.Commands;
using Application.EdgeIntegration.CommandDelivery.Results;
using Application.EdgeIntegration.Dispatch.Results;
using Application.EdgeIntegration.Reports.Results;
using Application.Orders.Abstractions;
using Application.Orders.Support;
using Application.Shared.Wrappers;
using Application.Tenants;

namespace Application.Orders.Management.Commands;

public sealed class RedispatchOrderExecutionCommandHandler
{
    private readonly IOrderStore _orderStore;
    private readonly DispatchOrderExecutionCommandHandler _dispatchHandler;
    private readonly IRealtimeNotificationPublisher _publisher;

    public RedispatchOrderExecutionCommandHandler(
        IOrderStore orderStore,
        DispatchOrderExecutionCommandHandler dispatchHandler,
        IRealtimeNotificationPublisher publisher)
    {
        _orderStore = orderStore;
        _dispatchHandler = dispatchHandler;
        _publisher = publisher;
    }

    public async Task<ApiResult<OrderExecutionDispatchResult>> HandleAsync(
        RedispatchOrderExecutionCommand command,
        CancellationToken cancellationToken = default)
    {
        var reason = command.Reason?.Trim();
        if (command.UserContext.AccountId == Guid.Empty || string.IsNullOrWhiteSpace(reason) || reason.Length > 500)
        {
            return ApiResult<OrderExecutionDispatchResult>.Fail(
                "Authenticated operator and redispatch reason of at most 500 characters are required.", 400);
        }

        var order = await _orderStore.GetOrderByIdAsync(command.OrderId, cancellationToken);
        if (order is null)
        {
            return ApiResult<OrderExecutionDispatchResult>.Fail("Order not found.", 404);
        }

        if (!ScopeAccessRules.CanAccessScopedRow(
                ScopeRoleSets.OrdersManage,
                command.UserContext,
                order.OrganizationId,
                order.StoreId,
                order.KioskId))
        {
            return ApiResult<OrderExecutionDispatchResult>.Fail("Access denied.", 403);
        }

        var previousStatus = order.Status;
        var result = await _dispatchHandler.HandleRedispatchAsync(
            order.Id,
            command.UserContext.AccountId,
            reason,
            cancellationToken);
        if (!result.Succeeded)
        {
            return result;
        }

        if (order.Status != previousStatus)
        {
            var projection = OrderStatusProjector.ProjectFromOrder(order);
            await _publisher.PublishOrderStatusChangedAsync(new OrderStatusChangedEvent
            {
                OrderId = order.Id,
                OrderNumber = order.OrderNumber,
                KioskId = order.KioskId,
                OrganizationId = order.OrganizationId,
                StoreId = order.StoreId,
                OldStatus = previousStatus.ToString(),
                NewStatus = order.Status.ToString(),
                PaymentStatus = order.PaymentStatus.ToString(),
                CustomerStatus = projection.CustomerStatus,
                CustomerStatusMessage = projection.CustomerStatusMessage,
                CanRetryPayment = projection.CanRetryPayment,
                RequiresStaffSupport = projection.RequiresStaffSupport,
                UpdatedAt = DateTimeOffset.UtcNow,
                Version = 1
            }, cancellationToken);
        }

        await _publisher.PublishDashboardInvalidatedAsync(new DashboardInvalidatedEvent
        {
            Scope = order.OrganizationId.HasValue ? "Organization" : "System",
            OrganizationId = order.OrganizationId,
            StoreId = order.StoreId,
            Reason = "OrderExecutionRedispatched",
            UpdatedAt = DateTimeOffset.UtcNow
        }, cancellationToken);

        return result;
    }
}
