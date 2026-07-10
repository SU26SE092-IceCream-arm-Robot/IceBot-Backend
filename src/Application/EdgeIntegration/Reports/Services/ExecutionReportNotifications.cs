using Application.Abstractions.Realtime.Events;

namespace Application.EdgeIntegration.Reports.Services;

internal sealed class ExecutionReportNotifications
{
    public OrderStatusChangedEvent? OrderStatusChanged { get; set; }
    public List<InventoryChangedEvent> InventoryChanged { get; } = [];
}
