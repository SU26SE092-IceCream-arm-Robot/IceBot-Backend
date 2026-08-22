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
    public static async Task<bool> ApplyAsync(
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
        var states = (await store.GetDispenserStatesAsync(
                command.StockMovements.Select(evidence => evidence.IngredientDispenserStateId).Distinct().ToArray(),
                cancellationToken))
            .ToDictionary(state => state.Id);
        await store.AcquireKioskIngredientInventoryMutationLocksAsync(
            states.Values.Select(state => state.KioskIngredientInventoryId), cancellationToken);
        var applied = false;

        foreach (var evidence in command.StockMovements)
        {
            var existingMovement = await store.GetStockMovementBySourceEventIdAsync(
                evidence.SourceEventId, cancellationToken);
            if (existingMovement is not null)
            {
                if (existingMovement.IngredientDispenserStateId != evidence.IngredientDispenserStateId ||
                    existingMovement.Quantity != -evidence.QuantityConsumed ||
                    existingMovement.ReferenceType != "OrderItem" ||
                    existingMovement.ReferenceId != evidence.OrderItemId ||
                    existingMovement.OriginNodeId != context.SourceExecutorId ||
                    existingMovement.IsEstimated != evidence.IsEstimated ||
                    !HasSameStoredTimestamp(
                        existingMovement.OccurredAt,
                        evidence.OccurredAt ?? command.EdgeCreatedAt) ||
                    evidence.BalanceAfter.HasValue && existingMovement.BalanceAfter != evidence.BalanceAfter)
                {
                    throw new DomainRuleException(
                        "Stock movement source event id was reused with different evidence.");
                }

                continue;
            }
            if (!states.TryGetValue(evidence.IngredientDispenserStateId, out var state))
                throw new DomainRuleException("Stock movement dispenser state was not found.");
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
            var balance = state.KioskIngredientInventory;
            var balanceBefore = balance.EstimatedQuantity;
            if (balanceBefore.HasValue && balance.ConsumeAvailable(evidence.QuantityConsumed, occurredAt) != evidence.QuantityConsumed)
                throw new DomainRuleException("Not enough canonical kiosk inventory for reported consumption.");
            state.ConsumeWithEvidence(
                evidence.QuantityConsumed,
                occurredAt,
                evidence.BalanceAfter,
                "OrderItem",
                evidence.OrderItemId,
                evidence.SourceEventId);
            var movement = StockMovement.CreateForKioskInventory(
                balance.Id, balance.OrganizationId, balance.StoreId, balance.KioskId, balance.IngredientId,
                "CONSUME", -evidence.QuantityConsumed, balanceBefore, balance.EstimatedQuantity, balance.Unit,
                occurredAt, "PRODUCTION_EXECUTION", "OrderItem", evidence.OrderItemId,
                evidence.SourceEventId, evidence.IsEstimated, state.Id);
            movement.OriginNodeId = context.SourceExecutorId;
            movement.Version = command.SequenceNumber;
            movement.CorrelationId = edgeCommand.OrderId;
            movement.CausationId = command.SourceEventId;
            await store.AddStockMovementAsync(movement, cancellationToken);
            applied = true;

            notifications.InventoryChanged.Add(new InventoryChangedEvent
            {
                DispenserStateId = state.Id,
                KioskId = state.KioskId!.Value,
                OrganizationId = state.Kiosk.OrganizationId,
                StoreId = state.Kiosk.StoreId,
                IngredientName = state.Ingredient.Name,
                EstimatedQuantity = state.EstimatedQuantity,
                Unit = state.Unit,
                Status = state.CurrentLevelStatus.ToString(),
                UpdatedAt = occurredAt,
                Version = 1
            });
        }

        return applied;
    }

    private static bool HasSameStoredTimestamp(DateTimeOffset left, DateTimeOffset right) =>
        left.UtcDateTime.Ticks / 10 == right.UtcDateTime.Ticks / 10;
}
