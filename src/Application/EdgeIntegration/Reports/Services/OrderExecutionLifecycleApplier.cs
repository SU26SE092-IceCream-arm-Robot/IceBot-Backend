using Application.Abstractions.Realtime.Events;
using Application.EdgeIntegration.Abstractions;
using Application.EdgeIntegration.CommandDelivery.Commands;
using Application.EdgeIntegration.Dispatch.Commands;
using Application.EdgeIntegration.Reports.Commands;
using Application.EdgeIntegration.Timeouts.Commands;
using Application.Orders.Support;
using Domain.Common;
using Domain.Orders.Entities;
using Domain.Orders.Enums;
using Domain.ProductionExecution.Enums;
using Domain.Sync.Entities;

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
    }
}
