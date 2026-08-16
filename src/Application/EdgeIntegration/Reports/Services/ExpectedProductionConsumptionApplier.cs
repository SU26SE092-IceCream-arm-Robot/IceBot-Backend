using System.Security.Cryptography;
using System.Text;
using Application.Abstractions.Realtime.Events;
using Application.EdgeIntegration.Abstractions;
using Domain.Common;
using Domain.Inventory.Entities;

namespace Application.EdgeIntegration.Reports.Services;

/// <summary>
/// Writes Cloud's expected inventory movement for completed production when the
/// Edge has no physical metering evidence. This is an estimate, not a claim
/// that Lua or the machine consumed an exact physical quantity.
/// </summary>
internal static class ExpectedProductionConsumptionApplier
{
    public static async Task<bool> ApplyAsync(
        IExecutionReportUnitOfWork store,
        ExecutionReportProcessingContext context,
        CancellationToken cancellationToken)
    {
        var command = context.Command;
        if (!command.SourceProductionJobId.HasValue || !command.OrderItemId.HasValue ||
            !command.ProductionUnitQuantity.HasValue || command.ProductionUnitQuantity.Value <= 0)
            return false;

        var requirements = await store.ListExpectedInventoryRequirementsAsync(
            context.EdgeCommand.OrderId!.Value, command.OrderItemId.Value, cancellationToken);
        if (requirements.Count == 0) return false;

        var candidates = new Dictionary<ExpectedInventoryRequirement, List<IngredientDispenserState>>();
        foreach (var requirement in requirements)
        {
            candidates[requirement] = await store.ListActiveDispenserStatesForExpectedConsumptionAsync(
                context.Endpoint.KioskId, requirement.IngredientId, requirement.Unit, cancellationToken);
        }

        var stateIds = candidates.Values.SelectMany(states => states).Select(state => state.Id).Distinct().ToArray();
        if (stateIds.Length == 0) return false;
        await store.AcquireDispenserMutationLocksAsync(stateIds, cancellationToken);

        // Reload after locks so a concurrent refill, adjustment, or completion cannot be lost.
        candidates.Clear();
        foreach (var requirement in requirements)
        {
            candidates[requirement] = await store.ListActiveDispenserStatesForExpectedConsumptionAsync(
                context.Endpoint.KioskId, requirement.IngredientId, requirement.Unit, cancellationToken);
        }

        var planned = new List<(ExpectedInventoryRequirement Requirement, IngredientDispenserState State, decimal Quantity, Guid SourceEventId)>();
        foreach (var requirement in requirements)
        {
            var required = requirement.Quantity * command.ProductionUnitQuantity.Value;
            var states = candidates[requirement];
            var existing = new Dictionary<Guid, decimal>();
            foreach (var state in states)
            {
                var sourceEventId = CreateSourceEventId(command.SourceProductionJobId.Value, requirement.IngredientId, state.Id);
                var movement = await store.GetStockMovementBySourceEventIdAsync(sourceEventId, cancellationToken);
                if (movement is not null)
                {
                    if (movement.ReferenceType != "OrderItemProductionUnit" || movement.ReferenceId != command.OrderItemId ||
                        movement.Quantity >= 0 || !movement.IsEstimated)
                    {
                        throw new DomainRuleException("Expected inventory source id was reused with different evidence.");
                    }
                    existing[state.Id] = -movement.Quantity;
                }
            }

            var remaining = required - existing.Values.Sum();
            if (remaining < 0) throw new DomainRuleException("Expected inventory evidence exceeds the completed production quantity.");
            if (remaining == 0) continue;

            // A successful physical outcome stays true even if a concurrent adjustment made the estimate insufficient.
            // Do not reject the report or fabricate a negative balance; an operator must reconcile the inventory estimate.
            var available = states.Sum(state => state.EstimatedQuantity ?? 0m);
            if (available < remaining || states.Any(state => !state.EstimatedQuantity.HasValue)) return false;

            foreach (var state in states)
            {
                if (remaining == 0) break;
                var alreadyApplied = existing.GetValueOrDefault(state.Id);
                var availableFromState = state.EstimatedQuantity!.Value;
                var amount = Math.Min(availableFromState, remaining);
                if (amount <= 0 || alreadyApplied > 0) continue;
                planned.Add((requirement, state, amount,
                    CreateSourceEventId(command.SourceProductionJobId.Value, requirement.IngredientId, state.Id)));
                remaining -= amount;
            }

            if (remaining != 0) return false;
        }

        var applied = false;
        foreach (var entry in planned)
        {
            var movement = entry.State.Consume(
                entry.Quantity,
                context.CloudReceivedAt,
                "OrderItemProductionUnit",
                command.OrderItemId,
                entry.SourceEventId);
            movement.OrganizationId = entry.State.Kiosk!.OrganizationId;
            movement.StoreId = entry.State.Kiosk.StoreId;
            movement.ReasonCode = "EXPECTED_PRODUCTION_CONSUMPTION";
            movement.IsEstimated = true;
            movement.CorrelationId = context.EdgeCommand.OrderId;
            movement.CausationId = command.SourceEventId;
            await store.AddStockMovementAsync(movement, cancellationToken);
            applied = true;

            context.Notifications.InventoryChanged.Add(new InventoryChangedEvent
            {
                DispenserStateId = entry.State.Id,
                KioskId = entry.State.KioskId!.Value,
                OrganizationId = entry.State.Kiosk.OrganizationId,
                StoreId = entry.State.Kiosk.StoreId,
                IngredientName = entry.State.Ingredient.Name,
                EstimatedQuantity = entry.State.EstimatedQuantity,
                Unit = entry.State.Unit,
                Status = entry.State.CurrentLevelStatus.ToString(),
                UpdatedAt = context.CloudReceivedAt,
                Version = 1
            });
        }

        return applied;
    }

    private static Guid CreateSourceEventId(Guid productionJobId, Guid ingredientId, Guid dispenserStateId)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(
            $"expected-production-consumption:{productionJobId:D}:{ingredientId:D}:{dispenserStateId:D}"));
        return new Guid(bytes.AsSpan(0, 16));
    }
}
