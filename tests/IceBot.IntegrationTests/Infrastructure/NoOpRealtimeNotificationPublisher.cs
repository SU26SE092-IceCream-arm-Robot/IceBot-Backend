using Application.Abstractions.Realtime;
using Application.Abstractions.Realtime.Events;

namespace IceBot.IntegrationTests.Infrastructure;

internal sealed class NoOpRealtimeNotificationPublisher : IRealtimeNotificationPublisher
{
    public List<OrderExecutionObservationChangedEvent> OrderExecutionObservationEvents { get; } = [];
    public List<DeviceEventCreatedEvent> DeviceEventCreatedEvents { get; } = [];
    public List<KioskStatusChangedEvent> KioskStatusChangedEvents { get; } = [];
    public List<ExecutionReadinessChangedEvent> ExecutionReadinessChangedEvents { get; } = [];
    public List<AlertChangedEvent> AlertChangedEvents { get; } = [];
    public List<OrderItemFulfillmentChangedEvent> OrderItemFulfillmentChangedEvents { get; } = [];

    public Task PublishOrderStatusChangedAsync(OrderStatusChangedEvent evt, CancellationToken ct = default) => Task.CompletedTask;
    public Task PublishOrderItemFulfillmentChangedAsync(OrderItemFulfillmentChangedEvent evt, CancellationToken ct = default)
    {
        OrderItemFulfillmentChangedEvents.Add(evt);
        return Task.CompletedTask;
    }
    public Task PublishOrderExecutionObservationChangedAsync(OrderExecutionObservationChangedEvent evt, CancellationToken ct = default)
    {
        OrderExecutionObservationEvents.Add(evt);
        return Task.CompletedTask;
    }
    public Task PublishPaymentStatusChangedAsync(PaymentStatusChangedEvent evt, CancellationToken ct = default) => Task.CompletedTask;
    public Task PublishKioskStatusChangedAsync(KioskStatusChangedEvent evt, CancellationToken ct = default)
    {
        KioskStatusChangedEvents.Add(evt);
        return Task.CompletedTask;
    }
    public Task PublishExecutionReadinessChangedAsync(ExecutionReadinessChangedEvent evt, CancellationToken ct = default)
    {
        ExecutionReadinessChangedEvents.Add(evt);
        return Task.CompletedTask;
    }
    public Task PublishDeviceEventCreatedAsync(DeviceEventCreatedEvent evt, CancellationToken ct = default)
    {
        DeviceEventCreatedEvents.Add(evt);
        return Task.CompletedTask;
    }
    public Task PublishAlertChangedAsync(AlertChangedEvent evt, CancellationToken ct = default)
    {
        AlertChangedEvents.Add(evt);
        return Task.CompletedTask;
    }
    public Task PublishMaintenanceTicketChangedAsync(MaintenanceTicketChangedEvent evt, CancellationToken ct = default) => Task.CompletedTask;
    public Task PublishInventoryChangedAsync(InventoryChangedEvent evt, CancellationToken ct = default) => Task.CompletedTask;
    public Task PublishDashboardInvalidatedAsync(DashboardInvalidatedEvent evt, CancellationToken ct = default) => Task.CompletedTask;
}
