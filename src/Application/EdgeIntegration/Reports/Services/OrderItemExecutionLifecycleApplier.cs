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
    public static async Task ApplyJobEvidenceAsync(
        IExecutionReportUnitOfWork store,
        ExecutionReportProcessingContext context,
        CancellationToken cancellationToken)
    {
        var order = await GetOrderAsync(store, context, cancellationToken);
        var payload = ExecuteOrderCommandPayloadCodec.DeserializeAndValidateFull(context.EdgeCommand.PayloadJson);
        var isRemake = string.Equals(payload.ExecutionIntent, "Remake", StringComparison.Ordinal);
        if (IsFinalBusinessState(order.Status) && order.Status != OrderStatus.FulfillmentIssue)
            return;
        var line = payload.OrderLines.Single(candidate => candidate.OrderItemId == context.Command.OrderItemId);
        var item = GetMachineItem(order, line.OrderItemId);
        var records = await store.ListProductionExecutionRecordsForOrderItemAsync(
            order.Id, line.OrderItemId, cancellationToken);
        var snapshot = ProductionUnitOutcomeSnapshot.CreateEffective(item.Quantity, records, context.EdgeCommand);
        var previousOrderStatus = order.Status;

        await ApplyItemStatusAsync(
            store, context, order, item, snapshot.AggregateStatus!.Value,
            completeItem: snapshot.HasCompleteCoverage && snapshot.CompletedQuantity == item.Quantity,
            isRemake,
            $"Production unit evidence: {Describe(snapshot)}.",
            cancellationToken);

        if (isRemake && order.Status == OrderStatus.FulfillmentIssue &&
            order.OrderItems.All(candidate => candidate.Status != OrderItemStatus.Failed))
            order.BeginFulfillmentRecovery();

        OrderFulfillmentAggregator.Apply(order, context.ExecutorReportedAt, context.Command.ErrorMessage);
        await RecordOrderChangeAsync(
            store, context, order, previousOrderStatus, snapshot.AggregateStatus.Value, cancellationToken);
    }

    public static async Task ApplyOrderSummaryAsync(
        IExecutionReportUnitOfWork store,
        ExecutionReportProcessingContext context,
        ProductionExecutionStatus reportedStatus,
        CancellationToken cancellationToken)
    {
        var order = await GetOrderAsync(store, context, cancellationToken);
        var payload = ExecuteOrderCommandPayloadCodec.DeserializeAndValidateFull(context.EdgeCommand.PayloadJson);
        var snapshots = new List<(ExecuteOrderLinePayload Line, ProductionUnitOutcomeSnapshot Snapshot)>();
        foreach (var line in payload.OrderLines)
        {
            var records = await store.ListProductionExecutionRecordsAsync(
                context.EdgeCommand.Id, line.OrderItemId, cancellationToken);
            snapshots.Add((line, ProductionUnitOutcomeSnapshot.Create(
                line.Quantity, records, line.ProductionUnitStartNo)));
        }

        var hasJobEvidence = snapshots.Any(entry => entry.Snapshot.HasEvidence);
        if (hasJobEvidence)
            EnsureSummaryMatchesJobEvidence(reportedStatus, snapshots);
        if (IsFinalBusinessState(order.Status)) return;

        var previousOrderStatus = order.Status;
        foreach (var entry in snapshots)
        {
            var item = GetMachineItem(order, entry.Line.OrderItemId);
            var effectiveStatus = hasJobEvidence
                ? entry.Snapshot.AggregateStatus
                    ?? throw new DomainRuleException(
                        "Order summary cannot finalize an order line without production-unit evidence.")
                : reportedStatus;
            await ApplyItemStatusAsync(
                store, context, order, item, effectiveStatus,
                completeItem: hasJobEvidence
                    ? entry.Snapshot.HasCompleteCoverage && entry.Snapshot.CompletedQuantity == entry.Line.Quantity
                    : reportedStatus == ProductionExecutionStatus.Completed,
                isRemake: false,
                hasJobEvidence
                    ? $"Edge order summary verified against unit evidence: {Describe(entry.Snapshot)}."
                    : $"Edge order summary without unit evidence: {reportedStatus}.",
                cancellationToken);
        }

        OrderFulfillmentAggregator.Apply(order, context.ExecutorReportedAt, context.Command.ErrorMessage);
        await RecordOrderChangeAsync(store, context, order, previousOrderStatus, reportedStatus, cancellationToken);
    }

    private static void EnsureSummaryMatchesJobEvidence(
        ProductionExecutionStatus reportedStatus,
        IReadOnlyCollection<(ExecuteOrderLinePayload Line, ProductionUnitOutcomeSnapshot Snapshot)> snapshots)
    {
        if (reportedStatus is not (ProductionExecutionStatus.Completed or ProductionExecutionStatus.Failed or
            ProductionExecutionStatus.RequiresManualIntervention))
            return;

        if (snapshots.Any(entry => !entry.Snapshot.HasCompleteCoverage))
            throw new DomainRuleException(
                "A final order summary requires complete production-unit evidence for every dispatched order line.");

        var expected = snapshots.Any(entry => entry.Snapshot.FailedQuantity > 0)
            ? ProductionExecutionStatus.Failed
            : snapshots.Any(entry => entry.Snapshot.ManualInterventionQuantity > 0)
                ? ProductionExecutionStatus.RequiresManualIntervention
                : snapshots.All(entry =>
                    entry.Snapshot.HasCompleteCoverage &&
                    entry.Snapshot.CompletedQuantity == entry.Line.Quantity)
                    ? ProductionExecutionStatus.Completed
                    : snapshots.Any(entry => entry.Snapshot.AggregateStatus == ProductionExecutionStatus.Running)
                        ? ProductionExecutionStatus.Running
                        : ProductionExecutionStatus.Accepted;

        var compatible = reportedStatus == expected ||
            reportedStatus == ProductionExecutionStatus.Failed &&
            expected == ProductionExecutionStatus.RequiresManualIntervention;
        if (!compatible)
            throw new DomainRuleException(
                $"Order summary status {reportedStatus} contradicts production-unit evidence status {expected}.");
    }

    private static async Task ApplyItemStatusAsync(
        IExecutionReportUnitOfWork store,
        ExecutionReportProcessingContext context,
        Order order,
        OrderItem item,
        ProductionExecutionStatus status,
        bool completeItem,
        bool isRemake,
        string reason,
        CancellationToken cancellationToken)
    {
        var previousItemStatus = item.Status;
        ApplyItemStatus(item, status, completeItem, isRemake);
        if (item.Status == previousItemStatus) return;

        await store.AddOrderItemStatusHistoryAsync(new OrderItemStatusHistory
        {
            OrderItemId = item.Id,
            SourceEventId = context.Command.SourceEventId,
            FromStatus = previousItemStatus,
            ToStatus = item.Status,
            ChangedAt = context.ExecutorReportedAt,
            Reason = reason
        }, cancellationToken);
        QueueItemChangedNotification(context, order, item, previousItemStatus);
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

    private static OrderItem GetMachineItem(Order order, Guid orderItemId)
    {
        var item = order.OrderItems.SingleOrDefault(candidate => candidate.Id == orderItemId)
            ?? throw new DomainRuleException("Dispatched order item was not found on the order.");
        if (item.FulfillmentType != FulfillmentType.MachineProduced)
            throw new DomainRuleException("Production execution reports apply only to machine-produced order items.");
        return item;
    }

    private static void ApplyItemStatus(
        OrderItem item,
        ProductionExecutionStatus status,
        bool completeItem,
        bool isRemake)
    {
        if (isRemake && status is (ProductionExecutionStatus.Accepted or ProductionExecutionStatus.Running) &&
            item.Status == OrderItemStatus.Failed)
        {
            item.MarkRemakePreparing();
            return;
        }

        if (isRemake && status == ProductionExecutionStatus.Completed && completeItem &&
            item.Status is OrderItemStatus.Failed or OrderItemStatus.Preparing)
        {
            item.MarkRemakeCompleted();
            return;
        }

        switch (status)
        {
            case ProductionExecutionStatus.Accepted when item.Status == OrderItemStatus.Pending:
                item.MarkAccepted(); break;
            case ProductionExecutionStatus.Running:
                if (item.Status == OrderItemStatus.Pending) item.MarkAccepted();
                if (item.Status == OrderItemStatus.Accepted) item.MarkPreparing();
                break;
            case ProductionExecutionStatus.Completed when completeItem &&
                item.Status is OrderItemStatus.Pending or OrderItemStatus.Accepted or OrderItemStatus.Preparing:
                if (item.Status == OrderItemStatus.Pending) item.MarkAccepted();
                item.MarkCompleted(); break;
            case ProductionExecutionStatus.Failed when item.Status is not (
                OrderItemStatus.Completed or OrderItemStatus.Cancelled or OrderItemStatus.Failed):
            case ProductionExecutionStatus.RequiresManualIntervention when item.Status is not (
                OrderItemStatus.Completed or OrderItemStatus.Cancelled or OrderItemStatus.Failed):
                item.MarkFailed(); break;
        }
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
            Reason = $"Production execution evidence: {status}."
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

    private static string Describe(ProductionUnitOutcomeSnapshot snapshot) =>
        $"completed={snapshot.CompletedQuantity}, failed={snapshot.FailedQuantity}, " +
        $"manualIntervention={snapshot.ManualInterventionQuantity}, inProgress={snapshot.InProgressQuantity}, " +
        $"unreported={snapshot.UnreportedQuantity}";

    private static bool IsFinalBusinessState(OrderStatus status) => status is
        OrderStatus.Completed or
        OrderStatus.Cancelled or
        OrderStatus.Failed or
        OrderStatus.ExecutionRejected or
        OrderStatus.RefundRequired or
        OrderStatus.Refunded or
        OrderStatus.Compensated or
        OrderStatus.FulfillmentIssue;
}
