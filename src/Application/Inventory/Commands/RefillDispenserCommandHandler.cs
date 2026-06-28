using Application.Abstractions.Realtime;
using Application.Abstractions.Realtime.Events;
using Application.Inventory.Abstractions;
using Application.Inventory.Mapping;
using Application.Inventory.Results;
using Application.Shared.Wrappers;
using Application.Tenants;

namespace Application.Inventory.Commands;

public sealed class RefillDispenserCommandHandler
{
    private readonly IInventoryStore _inventoryStore;
    private readonly IRealtimeNotificationPublisher _publisher;

    public RefillDispenserCommandHandler(IInventoryStore inventoryStore, IRealtimeNotificationPublisher publisher)
    {
        _inventoryStore = inventoryStore;
        _publisher = publisher;
    }

    public async Task<ApiResult<DispenserStateResult>> HandleAsync(
        RefillDispenserCommand command,
        CancellationToken cancellationToken = default)
    {
        if (command.Quantity <= 0)
        {
            return ApiResult<DispenserStateResult>.Fail("Refill quantity must be greater than zero.", 400);
        }

        Guid finalOrgId = Guid.Empty;
        Guid finalStoreId = Guid.Empty;

        var result = await _inventoryStore.ExecuteInTransactionAsync(async ct =>
        {
            var state = await _inventoryStore.GetDispenserStateByIdAsync(command.DispenserStateId, ct);
            if (state is null)
            {
                return ApiResult<DispenserStateResult>.Fail("Dispenser state not found.", 404);
            }

            var orgId = state.Kiosk?.OrganizationId;
            var storeId = state.Kiosk?.StoreId;
            var kioskId = state.KioskId;

            if (!ScopeAccessRules.CanAccessScopedRow(ScopeRoleSets.InventoryManage,
                command.UserContext,
                orgId,
                storeId,
                kioskId))
            {
                return ApiResult<DispenserStateResult>.Fail("Access denied.", 403);
            }

            finalOrgId = orgId ?? Guid.Empty;
            finalStoreId = storeId ?? Guid.Empty;

            var now = DateTimeOffset.UtcNow;
            var reasonCode = string.IsNullOrWhiteSpace(command.ReasonCode) ? "REFILL" : command.ReasonCode.Trim();

            // Perform domain action
            var movement = state.Refill(
                command.Quantity,
                now,
                reasonCode,
                sourceEventId: null,
                reportedLevelAfter: command.ReportedLevelAfter);

            // Enrich movement properties
            movement.Id = Guid.NewGuid();
            movement.OrganizationId = orgId;
            movement.StoreId = storeId;
            movement.CreatedByAccountId = command.UserContext.AccountId;
            movement.CreatedAt = now;

            await _inventoryStore.AddStockMovementAsync(movement, ct);
            await _inventoryStore.SaveChangesAsync(ct);

            return ApiResult<DispenserStateResult>.Success(
                DispenserStateResultMapper.ToResult(state),
                "Dispenser refilled successfully.");
        }, cancellationToken);

        if (result.Succeeded && result.Data is not null)
        {
            await _publisher.PublishInventoryChangedAsync(new InventoryChangedEvent
            {
                DispenserStateId = result.Data.Id,
                KioskId = result.Data.KioskId ?? Guid.Empty,
                OrganizationId = finalOrgId,
                StoreId = finalStoreId,
                IngredientName = result.Data.IngredientName,
                EstimatedQuantity = result.Data.EstimatedQuantity ?? 0,
                Unit = result.Data.Unit,
                Status = result.Data.CurrentLevelStatus.ToString(),
                UpdatedAt = result.Data.LastRefilledAt ?? DateTimeOffset.UtcNow,
                Version = 1
            }, cancellationToken);
        }

        return result;
    }
}
