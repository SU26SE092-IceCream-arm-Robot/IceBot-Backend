using Application.Abstractions.Realtime.Events;
using Application.EdgeIntegration.Abstractions;
using Application.EdgeIntegration.Commands;
using Application.Shared.Utils;
using Domain.Common;
using Domain.Orders.Entities;
using Domain.Orders.Enums;
using Domain.ProductionExecution.Enums;
using Domain.Sync.Entities;

namespace Application.EdgeIntegration.Services;

internal static class OrderExecutionLifecycleApplier
{
    public static async Task ApplyAsync(
        IExecutionReportUnitOfWork store,
        ExecutionReportProcessingContext context,
        ProductionExecutionStatus status,
        CancellationToken cancellationToken)
    {
        var command = context.Command;
        var edgeCommand = context.EdgeCommand;
        var executorReportedAt = context.ExecutorReportedAt;
        var notifications = context.Notifications;
        if (!edgeCommand.OrderId.HasValue) return;
        var order = await store.GetOrderAsync(edgeCommand.OrderId.Value, cancellationToken)
            ?? throw new DomainRuleException("Order for production execution report was not found.");
        var previousStatus = order.Status;
        switch (status)
        {
            case ProductionExecutionStatus.Accepted when order.Status == OrderStatus.ReadyForExecution:
                order.MarkAccepted(); break;
            case ProductionExecutionStatus.Running when order.Status is OrderStatus.ReadyForExecution or OrderStatus.Accepted:
                order.MarkPreparing(); break;
            case ProductionExecutionStatus.Completed when order.Status != OrderStatus.Completed:
                order.Complete(executorReportedAt); break;
            case ProductionExecutionStatus.Failed when order.Status != OrderStatus.Failed:
                order.MarkFailed(command.ErrorMessage); break;
            case ProductionExecutionStatus.RequiresManualIntervention when order.Status != OrderStatus.RefundRequired:
                order.MarkRefundRequired(command.ErrorMessage); break;
        }
        if (order.Status == previousStatus) return;

        var error = string.IsNullOrWhiteSpace(command.ErrorCode) ? null : $" Error: {command.ErrorCode.Trim()}.";
        await store.AddOrderStatusHistoryAsync(new OrderStatusHistory
        {
            OrderId = order.Id,
            FromStatus = previousStatus,
            ToStatus = order.Status,
            ChangedAt = executorReportedAt,
            Reason = $"Production execution report: {command.Status.Trim()}.{error}".Trim()
        }, cancellationToken);

        var projection = OrderStatusProjector.ProjectFromOrder(order);
        notifications.OrderStatusChanged = new OrderStatusChangedEvent
        {
            OrderId = order.Id, OrderNumber = order.OrderNumber, KioskId = order.KioskId,
            OrganizationId = order.OrganizationId, StoreId = order.StoreId,
            OldStatus = previousStatus.ToString(), NewStatus = order.Status.ToString(),
            PaymentStatus = order.PaymentStatus.ToString(), CustomerStatus = projection.CustomerStatus,
            CustomerStatusMessage = projection.CustomerStatusMessage, CanRetryPayment = projection.CanRetryPayment,
            RequiresStaffSupport = projection.RequiresStaffSupport, UpdatedAt = executorReportedAt, Version = 1
        };
    }
}
