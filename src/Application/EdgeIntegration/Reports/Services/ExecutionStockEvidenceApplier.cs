using Application.Abstractions.Realtime.Events;
using Application.EdgeIntegration.Abstractions;
using Application.EdgeIntegration.CommandDelivery.Commands;
using Application.EdgeIntegration.Dispatch.Commands;
using Application.EdgeIntegration.Reports.Commands;
using Application.EdgeIntegration.Timeouts.Commands;
using Domain.Common;
using Domain.Devices.ExecutionEndpoints;
using Domain.Inventory.Entities;
using Domain.Sync.Entities;

namespace Application.EdgeIntegration.Reports.Services;

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
        await store.AcquireDispenserMutationLocksAsync(
            command.StockMovements.Select(evidence => evidence.IngredientDispenserStateId),
            cancellationToken);

        foreach (var evidence in command.StockMovements)
        {
            var existingMovement = await store.GetStockMovementBySourceEventIdAsync(
                evidence.SourceEventId, cancellationToken);
            if (existingMovement is not null)
            {
                if (existingMovement.IngredientDispenserStateId != evidence.IngredientDispenserStateId ||
                    existingMovement.Quantity != -evidence.QuantityConsumed ||
                    existingMovement.ReferenceId != edgeCommand.OrderId ||
                    existingMovement.OriginNodeId != context.SourceExecutorId ||
                    existingMovement.IsEstimated != evidence.IsEstimated ||
                    evidence.BalanceAfter.HasValue && existingMovement.BalanceAfter != evidence.BalanceAfter)
                {
                    throw new DomainRuleException(
                        "Stock movement source event id was reused with different evidence.");
                }

                continue;
            }
            var state = await store.GetDispenserStateAsync(evidence.IngredientDispenserStateId, cancellationToken)
                ?? throw new DomainRuleException("Stock movement dispenser state was not found.");
            if (state.KioskId != endpoint.KioskId || state.Kiosk is null)
                throw new DomainRuleException("Stock movement dispenser state does not belong to the reporting kiosk.");
            if (!state.IsActive)
                throw new DomainRuleException("Stock movement dispenser state is retired.");
            if (!edgeCommand.OrderId.HasValue || !await store.IsIngredientExpectedForOrderItemAsync(
                    edgeCommand.OrderId.Value, evidence.OrderItemId, state.IngredientId, cancellationToken))
            {
                throw new DomainRuleException("Stock movement ingredient is not required by the dispatched order.");
            }

            var occurredAt = evidence.OccurredAt ?? command.EdgeCreatedAt;
            var movement = state.ConsumeWithEvidence(
                evidence.QuantityConsumed,
                occurredAt,
                evidence.BalanceAfter,
                "Order",
                edgeCommand.OrderId,
                evidence.SourceEventId);
            movement.OrganizationId = state.Kiosk.OrganizationId;
            movement.StoreId = state.Kiosk.StoreId;
            movement.ReasonCode = "PRODUCTION_EXECUTION";
            movement.IsEstimated = evidence.IsEstimated;
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
                EstimatedQuantity = state.EstimatedQuantity,
                Unit = state.Unit, Status = state.CurrentLevelStatus.ToString(), UpdatedAt = occurredAt, Version = 1
            });
        }
    }
}
