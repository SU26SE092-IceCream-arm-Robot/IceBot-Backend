using Application.Abstractions.Realtime.Events;

namespace Application.EdgeIntegration.Reports.Services;

internal sealed class ExecutionReportNotifications
{
    public OrderStatusChangedEvent? OrderStatusChanged { get; set; }
    public OrderExecutionObservationChangedEvent? OrderExecutionObservationChanged { get; set; }
    public List<OrderItemFulfillmentChangedEvent> OrderItemFulfillmentChanged { get; } = [];
    public List<InventoryChangedEvent> InventoryChanged { get; } = [];
}
