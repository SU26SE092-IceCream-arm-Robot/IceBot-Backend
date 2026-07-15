using Application.Abstractions.Realtime.Events;

namespace Application.Abstractions.Realtime;

public interface IRealtimeNotificationPublisher
{
    Task PublishOrderStatusChangedAsync(OrderStatusChangedEvent evt, CancellationToken ct = default);
    Task PublishOrderItemFulfillmentChangedAsync(OrderItemFulfillmentChangedEvent evt, CancellationToken ct = default);
    Task PublishOrderExecutionObservationChangedAsync(OrderExecutionObservationChangedEvent evt, CancellationToken ct = default);
    Task PublishPaymentStatusChangedAsync(PaymentStatusChangedEvent evt, CancellationToken ct = default);
    Task PublishKioskStatusChangedAsync(KioskStatusChangedEvent evt, CancellationToken ct = default);
    Task PublishExecutionReadinessChangedAsync(ExecutionReadinessChangedEvent evt, CancellationToken ct = default);
    Task PublishDeviceEventCreatedAsync(DeviceEventCreatedEvent evt, CancellationToken ct = default);
    Task PublishAlertChangedAsync(AlertChangedEvent evt, CancellationToken ct = default);
    Task PublishMaintenanceTicketChangedAsync(MaintenanceTicketChangedEvent evt, CancellationToken ct = default);
    Task PublishInventoryChangedAsync(InventoryChangedEvent evt, CancellationToken ct = default);
    Task PublishDashboardInvalidatedAsync(DashboardInvalidatedEvent evt, CancellationToken ct = default);
}
