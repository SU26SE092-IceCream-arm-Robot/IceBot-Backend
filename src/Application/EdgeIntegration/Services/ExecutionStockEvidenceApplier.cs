using Application.Abstractions.Realtime.Events;
using Application.EdgeIntegration.Abstractions;
using Application.EdgeIntegration.Commands;
using Domain.Common;
using Domain.Devices.ExecutionEndpoints;
using Domain.Inventory.Entities;
using Domain.Sync.Entities;

namespace Application.EdgeIntegration.Services;

internal static class ExecutionStockEvidenceApplier
{
    public static async Task ApplyAsync(
        IExecutionReportUnitOfWork store,
        ExecutionReportProcessingContext context,
        CancellationToken cancellationToken)
    {
        var command = context.Command;
        var endpoint = context.Endpoint;
        var edgeCommand = context.EdgeCommand;
        var notifications = context.Notifications;
        foreach (var evidence in command.StockMovements)
        {
            if (await store.StockMovementExistsAsync(evidence.SourceEventId, cancellationToken)) continue;
            var state = await store.GetDispenserStateAsync(evidence.IngredientDispenserStateId, cancellationToken)
                ?? throw new DomainRuleException("Stock movement dispenser state was not found.");
            if (state.KioskId != endpoint.KioskId || state.Kiosk is null)
                throw new DomainRuleException("Stock movement dispenser state does not belong to the reporting kiosk.");

            var occurredAt = evidence.OccurredAt ?? command.EdgeCreatedAt;
            if (evidence.BalanceAfter.HasValue)
                state.RecordSensorLevel(state.CurrentLevelStatus, occurredAt, estimatedQuantity: evidence.BalanceAfter.Value);

            var movement = StockMovement.Create(
                state.Id, state.Kiosk.OrganizationId, state.Kiosk.StoreId, state.KioskId, state.DeviceId,
                state.IngredientId, "CONSUME", -evidence.QuantityConsumed, evidence.BalanceAfter, state.Unit,
                occurredAt, "PRODUCTION_EXECUTION", "Order", edgeCommand.OrderId, evidence.SourceEventId,
                evidence.IsEstimated);
            movement.OriginNodeId = context.SourceExecutorId;
            movement.Version = command.SequenceNumber;
            movement.CorrelationId = edgeCommand.OrderId;
            movement.CausationId = command.SourceEventId;
            await store.AddStockMovementAsync(movement, cancellationToken);

            notifications.InventoryChanged.Add(new InventoryChangedEvent
            {
                DispenserStateId = state.Id, KioskId = state.KioskId!.Value,
                OrganizationId = state.Kiosk.OrganizationId, StoreId = state.Kiosk.StoreId,
                IngredientName = state.Ingredient.Name,
                EstimatedQuantity = state.EstimatedQuantity ?? evidence.BalanceAfter ?? 0,
                Unit = state.Unit, Status = state.CurrentLevelStatus.ToString(), UpdatedAt = occurredAt, Version = 1
            });
        }
    }
}
