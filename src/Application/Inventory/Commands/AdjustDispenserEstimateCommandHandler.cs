using Application.Inventory.Abstractions;
using Application.Inventory.Mapping;
using Application.Inventory.Results;
using Application.Shared.Wrappers;
using Application.Tenants;

namespace Application.Inventory.Commands;

public sealed class AdjustDispenserEstimateCommandHandler
{
    private readonly IInventoryStore _inventoryStore;

    public AdjustDispenserEstimateCommandHandler(IInventoryStore inventoryStore)
    {
        _inventoryStore = inventoryStore;
    }

    public async Task<ApiResult<DispenserStateResult>> HandleAsync(
        AdjustDispenserEstimateCommand command,
        CancellationToken cancellationToken = default)
    {
        if (command.EstimatedQuantity < 0)
        {
            return ApiResult<DispenserStateResult>.Fail("Estimated quantity cannot be negative.", 400);
        }

        return await _inventoryStore.ExecuteInTransactionAsync(async ct =>
        {
            var state = await _inventoryStore.GetDispenserStateByIdAsync(command.DispenserStateId, ct);
            if (state is null)
            {
                return ApiResult<DispenserStateResult>.Fail("Dispenser state not found.", 404);
            }

            var orgId = state.Kiosk?.OrganizationId;
            var storeId = state.Kiosk?.StoreId;
            var kioskId = state.KioskId;

            if (!ScopeAccessRules.CanAccessScopedRow(
                command.UserContext,
                orgId,
                storeId,
                kioskId))
            {
                return ApiResult<DispenserStateResult>.Fail("Access denied.", 403);
            }

            var now = DateTimeOffset.UtcNow;
            var reasonCode = string.IsNullOrWhiteSpace(command.ReasonCode) ? "ADJUST" : command.ReasonCode.Trim();

            // Perform domain action
            var movement = state.AdjustEstimate(
                command.EstimatedQuantity,
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
                "Dispenser estimate adjusted successfully.");
        }, cancellationToken);
    }
}
