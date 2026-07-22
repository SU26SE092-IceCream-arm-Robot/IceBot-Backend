using Application.Inventory.Abstractions;
using Application.Inventory.Mapping;
using Application.Inventory.Results;
using Application.Inventory.Support;
using Application.Shared.Wrappers;
using Application.Tenants;
using Domain.Common;
using Domain.Inventory.Enums;

namespace Application.Inventory.Commands;

public sealed class UpdateDispenserStateCommandHandler(IInventoryStore inventory)
{
    public async Task<ApiResult<DispenserStateResult>> HandleAsync(UpdateDispenserStateCommand command, CancellationToken ct = default)
    {
        try
        {
            return await inventory.ExecuteInTransactionAsync(
                cancellationToken => UpdateLockedAsync(command, cancellationToken), ct);
        }
        catch (DomainRuleException ex)
        {
            return ApiResult<DispenserStateResult>.Fail(ex.Message, 409);
        }
    }

    private async Task<ApiResult<DispenserStateResult>> UpdateLockedAsync(
        UpdateDispenserStateCommand command,
        CancellationToken ct)
    {
        await inventory.AcquireDispenserMutationLockAsync(command.DispenserStateId, ct);
        var state = await inventory.GetDispenserStateByIdAsync(command.DispenserStateId, ct);
        if (state?.Kiosk is null) return ApiResult<DispenserStateResult>.Fail("Dispenser state not found.", 404);
        if (state.KioskId != command.KioskId) return ApiResult<DispenserStateResult>.Fail("Dispenser state not found.", 404);
        if (!ScopeAccessRules.CanAccessScopedRow(ScopeRoleSets.InventoryConfigure, command.UserContext,
                state.Kiosk.OrganizationId, state.Kiosk.StoreId, state.KioskId))
            return ApiResult<DispenserStateResult>.Fail("Access denied.", 403);

        var profileError = DispenserLevelQuantityProfileContract.Validate(
            command.Request.LevelToQuantityProfile, command.Request.CapacityQuantity);
        if (profileError is not null) return ApiResult<DispenserStateResult>.Fail(profileError);
        var capabilityError = DispenserDeviceCapabilityRules.Validate(state.Device.DeviceModel);
        if (capabilityError is not null)
            return ApiResult<DispenserStateResult>.Fail(capabilityError, 409);
        if (!string.Equals(state.Unit, command.Request.Unit.Trim(), StringComparison.OrdinalIgnoreCase) &&
            (state.EstimatedQuantity.HasValue || await inventory.HasStockMovementsAsync(state.Id, ct)))
            return ApiResult<DispenserStateResult>.Fail(
                "Dispenser unit cannot change after quantity or stock history exists. Retire it and create a new state.", 409);
        var beforeCapacity = state.CapacityQuantity;
        var beforeUnit = state.Unit;
        state.ConfigureContainer(command.Request.CapacityQuantity, command.Request.Unit,
            DispenserLevelQuantityProfileContract.Serialize(command.Request.LevelToQuantityProfile));
        state.UpdatedAt = DateTimeOffset.UtcNow;
        state.UpdatedByAccountId = command.UserContext.AccountId;
        await inventory.AddTopologyChangeRecordAsync(
            InventoryTopologyAuditFactory.Create(
                state,
                InventoryTopologyChangeType.ConfigurationUpdated,
                command.Request.Reason,
                command.UserContext.AccountId,
                state.UpdatedAt.Value,
                state.IsActive,
                beforeCapacity,
                beforeUnit),
            ct);
        await inventory.SaveChangesAsync(ct);
        return ApiResult<DispenserStateResult>.Success(DispenserStateResultMapper.ToResult(state), "Dispenser configuration updated.");
    }
}
