using Application.EdgeIntegration.Abstractions;
using Application.EdgeIntegration.Reports.Commands;
using Domain.Common;
using Domain.Orders.Incidents;
using Domain.ProductionExecution.Enums;
using Domain.Sync.Entities;

namespace Application.EdgeIntegration.Reports.Services;

internal static class ProductionIncidentEvidenceRecorder
{
    public static async Task RecordIfRequiredAsync(
        IProductionExecutionReportStore store,
        EdgeCommand edgeCommand,
        IngestExecutionReportCommand command,
        ProductionExecutionStatus status,
        PhysicalOutputState physicalOutputState,
        DateTimeOffset recordedAt,
        CancellationToken cancellationToken)
    {
        if (!command.SourceProductionJobId.HasValue ||
            status is not (ProductionExecutionStatus.Failed or ProductionExecutionStatus.RequiresManualIntervention))
            return;
        if (!edgeCommand.OrderId.HasValue)
            throw new DomainRuleException("Production incident requires an order-owned execute command.");
        if (await store.GetProductionIncidentBySourceAsync(
                edgeCommand.Id, command.SourceProductionJobId.Value, cancellationToken) is not null)
            return;

        var order = await store.GetOrderAsync(edgeCommand.OrderId.Value, cancellationToken)
            ?? throw new DomainRuleException("Production incident order was not found.");
        var item = order.OrderItems.SingleOrDefault(candidate => candidate.Id == command.OrderItemId)
            ?? throw new DomainRuleException("Production incident order item was not found.");
        var trigger = status == ProductionExecutionStatus.RequiresManualIntervention
            ? ProductionIncidentTrigger.ManualInterventionRequired
            : physicalOutputState == PhysicalOutputState.Unknown
                ? ProductionIncidentTrigger.OutcomeUnknown
                : ProductionIncidentTrigger.ExecutionFailed;
        var incident = ProductionIncident.OpenFromExecution(
            order.OrganizationId, order.StoreId, order.KioskId, order.Id, item.Id,
            edgeCommand.Id, command.SourceProductionJobId.Value,
            command.ProductionUnitNo!.Value, command.ProductionUnitQuantity!.Value,
            trigger, physicalOutputState, order.OrderNumber, item.ProductNameSnapshot,
            item.ProductVariantNameSnapshot, command.ErrorCode, command.ErrorMessage, recordedAt);
        await store.AddProductionIncidentAsync(incident, cancellationToken);
    }
}
