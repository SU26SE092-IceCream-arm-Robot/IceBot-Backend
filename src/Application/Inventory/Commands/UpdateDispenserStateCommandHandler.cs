using Application.Inventory.Abstractions;
using Application.Inventory.Mapping;
using Application.Inventory.Results;
using Application.Inventory.Support;
using Application.Shared.Wrappers;
using Application.Tenants;
using Domain.Common;

namespace Application.Inventory.Commands;

public sealed class UpdateDispenserStateCommandHandler(IInventoryStore inventory)
{
    public async Task<ApiResult<DispenserStateResult>> HandleAsync(UpdateDispenserStateCommand command, CancellationToken ct = default)
    {
        var state = await inventory.GetDispenserStateByIdAsync(command.DispenserStateId, ct);
        if (state?.Kiosk is null) return ApiResult<DispenserStateResult>.Fail("Dispenser state not found.", 404);
        if (!ScopeAccessRules.CanAccessScopedRow(ScopeRoleSets.InventoryConfigure, command.UserContext,
                state.Kiosk.OrganizationId, state.Kiosk.StoreId, state.KioskId))
            return ApiResult<DispenserStateResult>.Fail("Access denied.", 403);

        var profileError = DispenserLevelQuantityProfileContract.Validate(
            command.Request.LevelToQuantityProfile, command.Request.CapacityQuantity);
        if (profileError is not null) return ApiResult<DispenserStateResult>.Fail(profileError);
        if (!string.Equals(state.Unit, command.Request.Unit.Trim(), StringComparison.OrdinalIgnoreCase) &&
            (state.EstimatedQuantity.HasValue || await inventory.HasStockMovementsAsync(state.Id, ct)))
            return ApiResult<DispenserStateResult>.Fail(
                "Dispenser unit cannot change after quantity or stock history exists. Retire it and create a new state.", 409);
        try
        {
            state.ConfigureContainer(command.Request.CapacityQuantity, command.Request.Unit,
                DispenserLevelQuantityProfileContract.Serialize(command.Request.LevelToQuantityProfile));
            state.UpdatedAt = DateTimeOffset.UtcNow;
            state.UpdatedByAccountId = command.UserContext.AccountId;
            await inventory.SaveChangesAsync(ct);
            return ApiResult<DispenserStateResult>.Success(DispenserStateResultMapper.ToResult(state), "Dispenser configuration updated.");
        }
        catch (DomainRuleException ex)
        {
            return ApiResult<DispenserStateResult>.Fail(ex.Message, 409);
        }
    }
}
