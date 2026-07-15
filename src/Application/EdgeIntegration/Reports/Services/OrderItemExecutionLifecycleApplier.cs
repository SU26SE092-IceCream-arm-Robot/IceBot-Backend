using Application.Abstractions.Realtime.Events;
using Application.EdgeIntegration.Abstractions;
using Application.EdgeIntegration.Dispatch.Contracts;
using Application.Orders.Support;
using Domain.Catalog.Enums;
using Domain.Common;
using Domain.Orders.Entities;
using Domain.Orders.Enums;
using Domain.ProductionExecution.Enums;

namespace Application.EdgeIntegration.Reports.Services;

internal static class OrderItemExecutionLifecycleApplier
{
    public static async Task ApplyJobReportAsync(
        IExecutionReportUnitOfWork store,
        ExecutionReportProcessingContext context,
        ProductionExecutionStatus status,
        CancellationToken cancellationToken)
    {
        var command = context.Command;
        var order = await GetOrderAsync(store, context, cancellationToken);
        var payload = ExecuteOrderCommandPayloadCodec.DeserializeAndValidateFull(context.EdgeCommand.PayloadJson);
        var line = payload.OrderLines.SingleOrDefault(candidate => candidate.OrderItemId == command.OrderItemId)
            ?? throw new DomainRuleException("Production job order item is not present in the dispatched command.");
        if (command.ProductionUnitNo!.Value + command.ProductionUnitQuantity!.Value - 1 > line.Quantity)
            throw new DomainRuleException("Production job unit range exceeds the dispatched order-line quantity.");

        var item = order.OrderItems.Single(candidate => candidate.Id == line.OrderItemId);
        EnsureMachineProduced(item);
        var previousItemStatus = item.Status;
        var previousOrderStatus = order.Status;
        ApplyItemStatus(item, status, completeItem:
            command.ProductionUnitNo == 1 && command.ProductionUnitQuantity == line.Quantity);
        await RecordChangesAsync(
            store, context, order, item, previousItemStatus, previousOrderStatus, status, cancellationToken);
    }

    public static async Task ApplyOrderSummaryAsync(
        IExecutionReportUnitOfWork store,
        ExecutionReportProcessingContext context,
        ProductionExecutionStatus status,
        CancellationToken cancellationToken)
    {
        var order = await GetOrderAsync(store, context, cancellationToken);
        var payload = ExecuteOrderCommandPayloadCodec.DeserializeAndValidateFull(context.EdgeCommand.PayloadJson);
        var previousOrderStatus = order.Status;
        foreach (var line in payload.OrderLines)
        {
            var item = order.OrderItems.SingleOrDefault(candidate => candidate.Id == line.OrderItemId)
                ?? throw new DomainRuleException("Dispatched order item was not found on the order.");
            EnsureMachineProduced(item);
            var previousItemStatus = item.Status;
            ApplyItemStatus(item, status, completeItem: status == ProductionExecutionStatus.Completed);
            if (item.Status != previousItemStatus)
            {
                await store.AddOrderItemStatusHistoryAsync(new OrderItemStatusHistory
                {
                    OrderItemId = item.Id,
                    SourceEventId = context.Command.SourceEventId,
                    FromStatus = previousItemStatus,
                    ToStatus = item.Status,
                    ChangedAt = context.ExecutorReportedAt,
                    Reason = $"Edge order summary: {status}."
                }, cancellationToken);
                QueueItemChangedNotification(context, order, item, previousItemStatus);
            }
        }

        OrderFulfillmentAggregator.Apply(order, context.ExecutorReportedAt, context.Command.ErrorMessage);
        await RecordOrderChangeAsync(store, context, order, previousOrderStatus, status, cancellationToken);
    }

    private static async Task<Order> GetOrderAsync(
        IExecutionReportUnitOfWork store,
        ExecutionReportProcessingContext context,
        CancellationToken cancellationToken)
    {
        if (!context.EdgeCommand.OrderId.HasValue)
            throw new DomainRuleException("Execute-order command is missing its order identity.");
        return await store.GetOrderAsync(context.EdgeCommand.OrderId.Value, cancellationToken)
            ?? throw new DomainRuleException("Order for production execution report was not found.");
    }

    private static void ApplyItemStatus(OrderItem item, ProductionExecutionStatus status, bool completeItem)
    {
        switch (status)
        {
            case ProductionExecutionStatus.Accepted when item.Status == OrderItemStatus.Pending:
                item.MarkAccepted(); break;
            case ProductionExecutionStatus.Running:
                if (item.Status == OrderItemStatus.Pending) item.MarkAccepted();
                if (item.Status == OrderItemStatus.Accepted) item.MarkPreparing();
                break;
            case ProductionExecutionStatus.Completed when completeItem && item.Status != OrderItemStatus.Completed:
                if (item.Status == OrderItemStatus.Pending) item.MarkAccepted();
                item.MarkCompleted(); break;
            case ProductionExecutionStatus.Failed when item.Status is not (OrderItemStatus.Completed or OrderItemStatus.Failed):
            case ProductionExecutionStatus.RequiresManualIntervention when item.Status is not (OrderItemStatus.Completed or OrderItemStatus.Failed):
                item.MarkFailed(); break;
        }
    }

    private static void EnsureMachineProduced(OrderItem item)
    {
        if (item.FulfillmentType != FulfillmentType.MachineProduced)
            throw new DomainRuleException(
                "Production execution reports apply only to machine-produced order items.");
    }

    private static async Task RecordChangesAsync(
        IExecutionReportUnitOfWork store,
        ExecutionReportProcessingContext context,
        Order order,
        OrderItem item,
        OrderItemStatus previousItemStatus,
        OrderStatus previousOrderStatus,
        ProductionExecutionStatus status,
        CancellationToken cancellationToken)
    {
        if (item.Status != previousItemStatus)
        {
            await store.AddOrderItemStatusHistoryAsync(new OrderItemStatusHistory
            {
                OrderItemId = item.Id,
                SourceEventId = context.Command.SourceEventId,
                FromStatus = previousItemStatus,
                ToStatus = item.Status,
                ChangedAt = context.ExecutorReportedAt,
                Reason = $"Production job report: {status}."
            }, cancellationToken);
            QueueItemChangedNotification(context, order, item, previousItemStatus);
        }

        OrderFulfillmentAggregator.Apply(order, context.ExecutorReportedAt, context.Command.ErrorMessage);
        await RecordOrderChangeAsync(store, context, order, previousOrderStatus, status, cancellationToken);
    }

    private static async Task RecordOrderChangeAsync(
        IExecutionReportUnitOfWork store,
        ExecutionReportProcessingContext context,
        Order order,
        OrderStatus previousOrderStatus,
        ProductionExecutionStatus status,
        CancellationToken cancellationToken)
    {
        if (order.Status == previousOrderStatus) return;
        await store.AddOrderStatusHistoryAsync(new OrderStatusHistory
        {
            OrderId = order.Id,
            FromStatus = previousOrderStatus,
            ToStatus = order.Status,
            ChangedAt = context.ExecutorReportedAt,
            Reason = $"Production execution report: {status}."
        }, cancellationToken);
        var projection = OrderStatusProjector.ProjectFromOrder(order);
        context.Notifications.OrderStatusChanged = new OrderStatusChangedEvent
        {
            OrderId = order.Id, OrderNumber = order.OrderNumber, KioskId = order.KioskId,
            OrganizationId = order.OrganizationId, StoreId = order.StoreId,
            OldStatus = previousOrderStatus.ToString(), NewStatus = order.Status.ToString(),
            PaymentStatus = order.PaymentStatus.ToString(), CustomerStatus = projection.CustomerStatus,
            CustomerStatusMessage = projection.CustomerStatusMessage, CanRetryPayment = projection.CanRetryPayment,
            RequiresStaffSupport = projection.RequiresStaffSupport,
            UpdatedAt = context.ExecutorReportedAt, Version = 1
        };
    }

    private static void QueueItemChangedNotification(
        ExecutionReportProcessingContext context,
        Order order,
        OrderItem item,
        OrderItemStatus oldStatus)
    {
        context.Notifications.OrderItemFulfillmentChanged.Add(new OrderItemFulfillmentChangedEvent
        {
            OrderId = order.Id,
            OrderItemId = item.Id,
            OrderNumber = order.OrderNumber,
            KioskId = order.KioskId,
            OrganizationId = order.OrganizationId,
            StoreId = order.StoreId,
            FulfillmentType = item.FulfillmentType.ToString(),
            OldStatus = oldStatus.ToString(),
            NewStatus = item.Status.ToString(),
            Quantity = item.Quantity,
            UpdatedAt = context.ExecutorReportedAt,
            Version = 1
        });
    }
}
