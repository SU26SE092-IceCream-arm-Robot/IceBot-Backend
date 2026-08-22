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

        var balances = new Dictionary<ExpectedInventoryRequirement, KioskIngredientInventory?>();
        foreach (var requirement in requirements)
        {
            balances[requirement] = await store.GetKioskIngredientInventoryForExpectedConsumptionAsync(
                context.Endpoint.KioskId, requirement.IngredientId, requirement.Unit, cancellationToken);
        }

        await store.AcquireKioskIngredientInventoryMutationLocksAsync(
            balances.Values.Where(balance => balance is not null).Select(balance => balance!.Id), cancellationToken);

        var applied = false;
        foreach (var requirement in requirements)
        {
            var required = requirement.Quantity * command.ProductionUnitQuantity.Value;
            var sourceEventId = CreateSourceEventId(command.SourceProductionJobId.Value, requirement.IngredientId);
            var existing = await store.GetStockMovementBySourceEventIdAsync(sourceEventId, cancellationToken);
            if (existing is not null)
            {
                if (existing.ReferenceType != "OrderItemProductionUnit" || existing.ReferenceId != command.OrderItemId || existing.Quantity >= 0 || !existing.IsEstimated)
                    throw new DomainRuleException("Expected inventory source id was reused with different evidence.");
                continue;
            }

            var balance = balances[requirement];
            var appliedQuantity = balance?.ConsumeAvailable(required, context.CloudReceivedAt) ?? 0m;
            if (balance is not null && appliedQuantity > 0)
            {
                var movement = StockMovement.CreateForKioskInventory(balance.Id, balance.OrganizationId, balance.StoreId, balance.KioskId,
                    balance.IngredientId, "CONSUME", -appliedQuantity, balance.EstimatedQuantity + appliedQuantity,
                    balance.EstimatedQuantity, balance.Unit, context.CloudReceivedAt, "EXPECTED_PRODUCTION_CONSUMPTION",
                    "OrderItemProductionUnit", command.OrderItemId, sourceEventId, true);
                movement.Id = Guid.NewGuid();
                movement.CorrelationId = context.EdgeCommand.OrderId;
                movement.CausationId = command.SourceEventId;
                await store.AddStockMovementAsync(movement, cancellationToken);
                applied = true;
                context.Notifications.InventoryChanged.Add(new InventoryChangedEvent
                {
                    DispenserStateId = Guid.Empty,
                    KioskId = balance.KioskId,
                    OrganizationId = balance.OrganizationId,
                    StoreId = balance.StoreId,
                    IngredientName = balance.Ingredient.Name,
                    EstimatedQuantity = balance.EstimatedQuantity,
                    Unit = balance.Unit,
                    Status = "Balance",
                    UpdatedAt = context.CloudReceivedAt,
                    Version = checked((int)balance.Version)
                });
            }

            if (appliedQuantity < required)
            {
                const string reasonCode = "EXPECTED_CONSUMPTION_UNRECONCILED";
                var caseExists = await store.GetInventoryReconciliationCaseAsync(sourceEventId, requirement.IngredientId, requirement.Unit, reasonCode, cancellationToken);
                if (caseExists is null)
                {
                    await store.AddInventoryReconciliationCaseAsync(new InventoryReconciliationCase
                    {
                        Id = Guid.NewGuid(),
                        OrganizationId = context.Endpoint.Kiosk.OrganizationId,
                        StoreId = context.Endpoint.Kiosk.StoreId,
                        KioskId = context.Endpoint.KioskId,
                        IngredientId = requirement.IngredientId,
                        KioskIngredientInventoryId = balance?.Id,
                        SourceEventId = sourceEventId,
                        ExpectedQuantity = required,
                        AppliedQuantity = appliedQuantity,
                        Unit = requirement.Unit.Trim().ToLowerInvariant(),
                        ReasonCode = reasonCode,
                        CreatedAt = context.CloudReceivedAt
                    }, cancellationToken);
                }
            }
        }

        return applied;
    }

    private static Guid CreateSourceEventId(Guid productionJobId, Guid ingredientId)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(
            $"expected-production-consumption:{productionJobId:D}:{ingredientId:D}"));
        return new Guid(bytes.AsSpan(0, 16));
    }
}
