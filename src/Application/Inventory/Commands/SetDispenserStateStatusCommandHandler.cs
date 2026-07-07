using Application.Inventory.Abstractions;
using Application.Inventory.Mapping;
using Application.Inventory.Results;
using Application.Shared.Wrappers;
using Application.Tenants;
using Domain.Devices.Enums;
using Application.Inventory.Support;
using Domain.Inventory.Enums;

namespace Application.Inventory.Commands;

public sealed class SetDispenserStateStatusCommandHandler(IInventoryStore inventory)
{
    public async Task<ApiResult<DispenserStateResult>> HandleAsync(SetDispenserStateStatusCommand command, CancellationToken ct = default)
    {
        var state = await inventory.GetDispenserStateByIdAsync(command.DispenserStateId, ct);
        if (state?.Kiosk is null) return ApiResult<DispenserStateResult>.Fail("Dispenser state not found.", 404);
        if (!ScopeAccessRules.CanAccessScopedRow(ScopeRoleSets.InventoryConfigure, command.UserContext,
                state.Kiosk.OrganizationId, state.Kiosk.StoreId, state.KioskId))
            return ApiResult<DispenserStateResult>.Fail("Access denied.", 403);

        var now = DateTimeOffset.UtcNow;
        var beforeIsActive = state.IsActive;
        if (command.IsActive)
        {
            if (state.Device.Status == DeviceStatus.Retired)
                return ApiResult<DispenserStateResult>.Fail("Dispenser state cannot be reactivated on a retired device.", 409);
            if (!state.Ingredient.IsActive)
                return ApiResult<DispenserStateResult>.Fail("Dispenser state cannot be reactivated with an inactive ingredient.", 409);
            var capabilityError = DispenserDeviceCapabilityRules.Validate(state.Device.DeviceModel);
            if (capabilityError is not null)
                return ApiResult<DispenserStateResult>.Fail(capabilityError, 409);
            state.Reactivate(command.UserContext.AccountId, now);
        }
        else state.Retire(command.UserContext.AccountId, now);
        if (beforeIsActive != state.IsActive)
        {
            await inventory.AddTopologyChangeRecordAsync(
                InventoryTopologyAuditFactory.Create(
                    state,
                    state.IsActive ? InventoryTopologyChangeType.Reactivated : InventoryTopologyChangeType.Retired,
                    command.Reason,
                    command.UserContext.AccountId,
                    now,
                    beforeIsActive,
                    state.CapacityQuantity,
                    state.Unit),
                ct);
        }
        await inventory.SaveChangesAsync(ct);
        return ApiResult<DispenserStateResult>.Success(DispenserStateResultMapper.ToResult(state), "Dispenser status updated.");
    }
}
