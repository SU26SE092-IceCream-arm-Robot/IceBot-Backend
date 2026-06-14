using Microsoft.AspNetCore.SignalR;
using WebAPI.SignalR.Hubs;

namespace WebAPI.SignalR;

public sealed class SignalRNotificationPublisher : IRealtimeNotificationPublisher
{
    private readonly IHubContext<OrderHub> _orderHubContext;
    private readonly IHubContext<OperationsHub> _operationsHubContext;
    private readonly IHubContext<ManagementDashboardHub> _dashboardHubContext;
    private readonly ILogger<SignalRNotificationPublisher> _logger;

    public SignalRNotificationPublisher(
        IHubContext<OrderHub> orderHubContext,
        IHubContext<OperationsHub> operationsHubContext,
        IHubContext<ManagementDashboardHub> dashboardHubContext,
        ILogger<SignalRNotificationPublisher> logger)
    {
        _orderHubContext = orderHubContext;
        _operationsHubContext = operationsHubContext;
        _dashboardHubContext = dashboardHubContext;
        _logger = logger;
    }

    public async Task PublishOrderStatusChangedAsync(OrderStatusChangedEvent evt, CancellationToken ct = default)
    {
        try
        {
            await _orderHubContext.Clients.Group($"order:{evt.OrderId}").SendAsync("OrderStatusChanged", evt, ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to publish SignalR OrderStatusChanged event for order {OrderId}.", evt.OrderId);
        }

        var dashboardEvt = new DashboardInvalidatedEvent
        {
            Scope = "Organization",
            OrganizationId = evt.OrganizationId,
            StoreId = evt.StoreId,
            Reason = "OrderStatusChanged",
            UpdatedAt = evt.UpdatedAt
        };
        await PublishDashboardInvalidatedAsync(dashboardEvt, ct);
    }

    public async Task PublishPaymentStatusChangedAsync(PaymentStatusChangedEvent evt, CancellationToken ct = default)
    {
        try
        {
            await _orderHubContext.Clients.Group($"order:{evt.OrderId}").SendAsync("PaymentStatusChanged", evt, ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to publish SignalR PaymentStatusChanged event for order {OrderId}.", evt.OrderId);
        }

        var dashboardEvt = new DashboardInvalidatedEvent
        {
            Scope = "Organization",
            OrganizationId = evt.OrganizationId,
            StoreId = evt.StoreId,
            Reason = "PaymentStatusChanged",
            UpdatedAt = evt.UpdatedAt
        };
        await PublishDashboardInvalidatedAsync(dashboardEvt, ct);
    }

    public async Task PublishKioskStatusChangedAsync(KioskStatusChangedEvent evt, CancellationToken ct = default)
    {
        try
        {
            await _operationsHubContext.Clients.Group($"kiosk:{evt.KioskId}").SendAsync("KioskStatusChanged", evt, ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to publish SignalR KioskStatusChanged event for kiosk {KioskId}.", evt.KioskId);
        }

        var dashboardEvt = new DashboardInvalidatedEvent
        {
            Scope = "Organization",
            OrganizationId = evt.OrganizationId,
            StoreId = evt.StoreId,
            Reason = "KioskStatusChanged",
            UpdatedAt = evt.UpdatedAt
        };
        await PublishDashboardInvalidatedAsync(dashboardEvt, ct);
    }

    public async Task PublishDeviceEventCreatedAsync(DeviceEventCreatedEvent evt, CancellationToken ct = default)
    {
        try
        {
            await _operationsHubContext.Clients.Group($"kiosk:{evt.KioskId}").SendAsync("DeviceEventCreated", evt, ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to publish SignalR DeviceEventCreated event for kiosk {KioskId}.", evt.KioskId);
        }
    }

    public async Task PublishMaintenanceTicketChangedAsync(MaintenanceTicketChangedEvent evt, CancellationToken ct = default)
    {
        try
        {
            await _operationsHubContext.Clients.Group($"kiosk:{evt.KioskId}").SendAsync("MaintenanceTicketChanged", evt, ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to publish SignalR MaintenanceTicketChanged event for ticket {TicketId}.", evt.TicketId);
        }

        var dashboardEvt = new DashboardInvalidatedEvent
        {
            Scope = "Organization",
            OrganizationId = evt.OrganizationId,
            StoreId = evt.StoreId,
            Reason = "MaintenanceTicketChanged",
            UpdatedAt = evt.UpdatedAt
        };
        await PublishDashboardInvalidatedAsync(dashboardEvt, ct);
    }

    public async Task PublishInventoryChangedAsync(InventoryChangedEvent evt, CancellationToken ct = default)
    {
        try
        {
            await _operationsHubContext.Clients.Group($"kiosk:{evt.KioskId}").SendAsync("InventoryChanged", evt, ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to publish SignalR InventoryChanged event for dispenser state {DispenserStateId}.", evt.DispenserStateId);
        }

        var dashboardEvt = new DashboardInvalidatedEvent
        {
            Scope = "Organization",
            OrganizationId = evt.OrganizationId,
            StoreId = evt.StoreId,
            Reason = "InventoryChanged",
            UpdatedAt = evt.UpdatedAt
        };
        await PublishDashboardInvalidatedAsync(dashboardEvt, ct);
    }

    public async Task PublishDashboardInvalidatedAsync(DashboardInvalidatedEvent evt, CancellationToken ct = default)
    {
        try
        {
            await _dashboardHubContext.Clients.Group("dashboard:system").SendAsync("DashboardInvalidated", evt, ct);

            if (evt.OrganizationId.HasValue)
            {
                await _dashboardHubContext.Clients.Group($"dashboard:organization:{evt.OrganizationId.Value}").SendAsync("DashboardInvalidated", evt, ct);
            }

            if (evt.StoreId.HasValue)
            {
                await _dashboardHubContext.Clients.Group($"dashboard:store:{evt.StoreId.Value}").SendAsync("DashboardInvalidated", evt, ct);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to publish SignalR DashboardInvalidated event. Reason={Reason}", evt.Reason);
        }
    }
}
