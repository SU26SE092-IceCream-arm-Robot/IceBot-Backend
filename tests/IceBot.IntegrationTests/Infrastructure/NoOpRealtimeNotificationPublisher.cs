using Application.Abstractions.Realtime;
using Application.Abstractions.Realtime.Events;

namespace IceBot.IntegrationTests.Infrastructure;

internal sealed class NoOpRealtimeNotificationPublisher : IRealtimeNotificationPublisher
{
    public Task PublishOrderStatusChangedAsync(OrderStatusChangedEvent evt, CancellationToken ct = default) => Task.CompletedTask;
    public Task PublishPaymentStatusChangedAsync(PaymentStatusChangedEvent evt, CancellationToken ct = default) => Task.CompletedTask;
    public Task PublishKioskStatusChangedAsync(KioskStatusChangedEvent evt, CancellationToken ct = default) => Task.CompletedTask;
    public Task PublishDeviceEventCreatedAsync(DeviceEventCreatedEvent evt, CancellationToken ct = default) => Task.CompletedTask;
    public Task PublishMaintenanceTicketChangedAsync(MaintenanceTicketChangedEvent evt, CancellationToken ct = default) => Task.CompletedTask;
    public Task PublishInventoryChangedAsync(InventoryChangedEvent evt, CancellationToken ct = default) => Task.CompletedTask;
    public Task PublishDashboardInvalidatedAsync(DashboardInvalidatedEvent evt, CancellationToken ct = default) => Task.CompletedTask;
}
