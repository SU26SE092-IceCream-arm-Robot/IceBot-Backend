using Application.Inventory.Abstractions;
using Application.Inventory.Mapping;
using Application.Inventory.Results;
using Application.Inventory.Support;
using Application.Shared.Wrappers;
using Application.Tenants;
using Domain.Devices.Catalog;
using Domain.Inventory.Entities;
using Domain.Inventory.Enums;

namespace Application.Inventory.Commands;

public sealed class CreateDispenserStateCommandHandler(IInventoryStore inventory)
{
    public async Task<ApiResult<DispenserStateResult>> HandleAsync(CreateDispenserStateCommand command, CancellationToken ct = default)
    {
        var request = command.Request;
        if (request.DeviceId == Guid.Empty || request.IngredientId == Guid.Empty)
            return ApiResult<DispenserStateResult>.Fail("Device and ingredient are required.");

        var device = await inventory.GetDeviceForTopologyAsync(command.KioskId, request.DeviceId, ct);
        if (device?.Kiosk is null) return ApiResult<DispenserStateResult>.Fail("Device not found in the route kiosk.", 404);
        if (device.Status == DeviceStatus.Retired)
            return ApiResult<DispenserStateResult>.Fail("Retired device cannot own a dispenser state.", 409);
        var capabilityError = DispenserDeviceCapabilityRules.Validate(device.DeviceModel);
        if (capabilityError is not null)
            return ApiResult<DispenserStateResult>.Fail(capabilityError, 409);
        if (!ScopeAccessRules.CanAccessScopedRow(ScopeRoleSets.InventoryConfigure, command.UserContext,
                device.Kiosk.OrganizationId, device.Kiosk.StoreId, device.KioskId))
            return ApiResult<DispenserStateResult>.Fail("Access denied.", 403);

        var ingredient = await inventory.GetIngredientForTopologyAsync(request.IngredientId, ct);
        if (ingredient is null) return ApiResult<DispenserStateResult>.Fail("Ingredient not found.", 404);
        if (!ingredient.IsActive) return ApiResult<DispenserStateResult>.Fail("Inactive ingredient cannot be bound to a dispenser.", 409);

        var profileError = DispenserLevelQuantityProfileContract.Validate(request.LevelToQuantityProfile, request.CapacityQuantity);
        if (profileError is not null) return ApiResult<DispenserStateResult>.Fail(profileError);
        var containerCode = request.ContainerCode.Trim().ToUpperInvariant();
        if (await inventory.DispenserIdentityExistsAsync(device.Id, containerCode, cancellationToken: ct))
            return ApiResult<DispenserStateResult>.Fail("Container code already exists for this device.", 409);

        var now = DateTimeOffset.UtcNow;
        var state = new IngredientDispenserState
        {
            DeviceId = device.Id,
            KioskId = command.KioskId,
            IngredientId = ingredient.Id,
            ContainerCode = containerCode,
            CurrentLevelStatus = IngredientLevelStatus.Unknown,
            LastMeasuredAt = now,
            IsActive = true,
            OriginNodeId = Guid.Empty,
            Version = 1,
            CreatedAt = now,
            CreatedByAccountId = command.UserContext.AccountId
        };
        state.ConfigureContainer(request.CapacityQuantity, request.Unit,
            DispenserLevelQuantityProfileContract.Serialize(request.LevelToQuantityProfile));
        await inventory.AddDispenserStateAsync(state, ct);
        await inventory.AddTopologyChangeRecordAsync(
            InventoryTopologyAuditFactory.Create(
                state,
                InventoryTopologyChangeType.Created,
                "INITIAL_PROVISIONING",
                command.UserContext.AccountId,
                now),
            ct);
        if (!await inventory.TrySaveChangesAsync(ct))
            return ApiResult<DispenserStateResult>.Fail("Container code already exists for this device.", 409);

        var created = await inventory.GetDispenserStateByIdAsync(state.Id, ct)
                      ?? throw new InvalidOperationException("Created dispenser state could not be reloaded.");
        return ApiResult<DispenserStateResult>.Success(DispenserStateResultMapper.ToResult(created), "Dispenser state created.", 201);
    }
}
