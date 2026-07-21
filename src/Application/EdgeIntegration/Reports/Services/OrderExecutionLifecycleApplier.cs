using Application.Abstractions.Realtime.Events;
using Application.EdgeIntegration.Abstractions;
using Application.Orders.Support;
using Domain.Common;
using Domain.ProductionExecution.Enums;

namespace Application.EdgeIntegration.Reports.Services;

internal static class OrderExecutionLifecycleApplier
{
    public static async Task ApplyAsync(
        IExecutionReportUnitOfWork store,
        ExecutionReportProcessingContext context,
        ProductionExecutionStatus status,
        CancellationToken cancellationToken)
    {
        await OrderItemExecutionLifecycleApplier.ApplyOrderSummaryAsync(
            store, context, status, cancellationToken);

        var orderId = context.EdgeCommand.OrderId
            ?? throw new DomainRuleException("Execute-order command is missing its order identity.");
        var order = await store.GetOrderAsync(orderId, cancellationToken)
            ?? throw new DomainRuleException("Order for production execution report was not found.");
        var record = await store.GetOrderExecutionRecordAsync(context.EdgeCommand.Id, cancellationToken)
            ?? throw new DomainRuleException("Order execution projection was not found after applying its report.");
        var projection = OrderStatusProjector.ProjectFromOrderAndExecution(order, record);
        context.Notifications.OrderExecutionObservationChanged = new OrderExecutionObservationChangedEvent
        {
            OrderId = order.Id,
            OrderNumber = order.OrderNumber,
            KioskId = order.KioskId,
            OrganizationId = order.OrganizationId,
            StoreId = order.StoreId,
            ObservationStatus = record.ObservationStatus.ToString(),
            CustomerExecutionStatus = record.CustomerExecutionStatus.ToString(),
            CustomerStatus = projection.CustomerStatus,
            CustomerStatusMessage = projection.CustomerStatusMessage,
            RequiresStaffSupport = projection.RequiresStaffSupport,
            LastExecutorReportedAt = record.LastExecutorReportedAt,
            UpdatedAt = context.CloudReceivedAt,
            Version = 1
        };
    }
}
