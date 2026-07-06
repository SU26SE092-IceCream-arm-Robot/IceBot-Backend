using Application.Inventory.Abstractions;
using Application.Shared.Wrappers;
using Application.Tenants;

namespace Application.Inventory.Commands;

public sealed class DeleteDispenserStateCommandHandler(IInventoryStore inventory)
{
    public async Task<ApiResult<bool>> HandleAsync(DeleteDispenserStateCommand command, CancellationToken ct = default)
    {
        var state = await inventory.GetDispenserStateByIdAsync(command.DispenserStateId, ct);
        if (state?.Kiosk is null) return ApiResult<bool>.Fail("Dispenser state not found.", 404);
        if (!ScopeAccessRules.CanAccessScopedRow(ScopeRoleSets.InventoryConfigure, command.UserContext,
                state.Kiosk.OrganizationId, state.Kiosk.StoreId, state.KioskId))
            return ApiResult<bool>.Fail("Access denied.", 403);
        if (await inventory.HasStockMovementsAsync(state.Id, ct))
            return ApiResult<bool>.Fail("Dispenser state has stock history and must be retired instead of deleted.", 409);

        inventory.RemoveDispenserState(state);
        await inventory.SaveChangesAsync(ct);
        return ApiResult<bool>.Success(true, "Unused dispenser state deleted.");
    }
}
