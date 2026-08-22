using Application.Devices.Catalog.Abstractions;
using Application.Tenants;
using Application.Devices.ExecutionEndpoints.Abstractions;
using Application.Devices.Telemetry.Abstractions;
using Application.Devices.Connectivity.Abstractions;
using Application.Devices.Credentials.Abstractions;
using Application.Devices.Catalog.Results;
using Application.Devices.ExecutionEndpoints.Results;
using Application.Devices.Telemetry.Results;
using Application.Devices.Connectivity.Results;
using Application.Devices.Credentials.Results;
using Application.Inventory.Abstractions;
using Application.Inventory.Support;
using Application.Shared.Wrappers;
using Application.Tenants.Kiosks;
using Domain.Devices.Catalog;
using Domain.Inventory.Entities;
using Domain.Inventory.Enums;

namespace Application.Devices.Catalog.Commands;

public sealed class ReplaceDeviceCommandHandler(
    IDeviceManagementStore devices,
    IInventoryStore inventory)
{
    public async Task<ApiResult<DeviceReplacementResult>> HandleAsync(
        ReplaceDeviceCommand command,
        CancellationToken cancellationToken = default) =>
        await inventory.ExecuteInTransactionAsync(
            ct => ReplaceLockedAsync(command, ct), cancellationToken);

    private async Task<ApiResult<DeviceReplacementResult>> ReplaceLockedAsync(
        ReplaceDeviceCommand command,
        CancellationToken cancellationToken)
    {
        await inventory.AcquireDeviceTopologyMutationLocksAsync(
            [command.SourceDeviceId, command.Request.ReplacementDeviceId],
            cancellationToken);
        var source = await devices.GetByKioskIdAsync(command.KioskId, command.SourceDeviceId, cancellationToken);
        var target = await devices.GetByKioskIdAsync(command.KioskId, command.Request.ReplacementDeviceId, cancellationToken);
        if (source?.Kiosk is null || target?.Kiosk is null)
        {
            return ApiResult<DeviceReplacementResult>.Fail("Source or replacement device not found.", 404);
        }
        if (source.Id == target.Id)
        {
            return ApiResult<DeviceReplacementResult>.Fail("Replacement device must differ from the source device.", 400);
        }
        if (!KioskAccessRules.CanAccessKiosk(ScopeRoleSets.DevicesManage, command.UserContext, source.Kiosk))
        {
            return ApiResult<DeviceReplacementResult>.Fail("Access denied.", 403);
        }
        if (source.Status == DeviceStatus.Retired || target.Status == DeviceStatus.Retired)
        {
            return ApiResult<DeviceReplacementResult>.Fail("Retired devices cannot participate in replacement.", 409);
        }

        if (await inventory.HasActiveExecutionAsync(source.Kiosk.Id, cancellationToken))
        {
            return ApiResult<DeviceReplacementResult>.Fail(
                "Device cannot be replaced while its kiosk has an accepted or running execution.", 409);
        }

        var sourceStateIds = await inventory.ListActiveDispenserStateIdsByDeviceAsync(source.Id, cancellationToken);
        foreach (var sourceStateId in sourceStateIds.OrderBy(id => id))
        {
            await inventory.AcquireDispenserMutationLockAsync(sourceStateId, cancellationToken);
        }

        var sourceStates = await inventory.ListActiveDispenserStatesByDeviceAsync(source.Id, cancellationToken);
        if (sourceStates.Count > 0)
        {
            var capabilityError = DispenserDeviceCapabilityRules.Validate(target.DeviceModel);
            if (capabilityError is not null)
            {
                return ApiResult<DeviceReplacementResult>.Fail(capabilityError, 409);
            }
        }

        foreach (var sourceState in sourceStates.OrderBy(state => state.Id))
        {
            if (await inventory.DispenserIdentityExistsAsync(
                    target.Id,
                    sourceState.ContainerCode,
                    cancellationToken: cancellationToken))
            {
                return ApiResult<DeviceReplacementResult>.Fail(
                    $"Replacement device already has active container '{sourceState.ContainerCode}'.", 409);
            }
        }

        var now = DateTimeOffset.UtcNow;
        var replacements = new List<IngredientDispenserState>(sourceStates.Count);
        foreach (var sourceState in sourceStates)
        {
            var previousEstimate = sourceState.EstimatedQuantity;
            var previousLevel = sourceState.CurrentLevelStatus;
            var replacement = new IngredientDispenserState
            {
                DeviceId = target.Id,
                KioskId = sourceState.KioskId,
                IngredientId = sourceState.IngredientId,
                KioskIngredientInventoryId = sourceState.KioskIngredientInventoryId,
                ContainerCode = sourceState.ContainerCode,
                CurrentLevelStatus = previousLevel,
                LastMeasuredAt = now,
                IsActive = true,
                OriginNodeId = Guid.Empty,
                Version = 1,
                CreatedAt = now,
                CreatedByAccountId = command.UserContext.AccountId
            };
            replacement.ConfigureContainer(
                sourceState.CapacityQuantity,
                sourceState.Unit,
                sourceState.LevelToQuantityProfileJson);
            replacement.ChangeTrackingMode(sourceState.TrackingMode);

            var transferredQuantity = 0m;
            if (previousEstimate is > 0)
            {
                sourceState.AdjustEstimate(
                    0,
                    now,
                    "DEVICE_REPLACEMENT_TRANSFER_OUT",
                    reportedLevelAfter: IngredientLevelStatus.Unknown);

                transferredQuantity = previousEstimate.Value;
                replacement.AdjustEstimate(
                    transferredQuantity,
                    now,
                    "DEVICE_REPLACEMENT_TRANSFER_IN",
                    reportedLevelAfter: previousLevel);
            }

            sourceState.Retire(command.UserContext.AccountId, now);
            await inventory.AddDispenserStateAsync(replacement, cancellationToken);
            await inventory.AddTopologyRebindRecordAsync(new InventoryTopologyRebindRecord
            {
                KioskId = source.Kiosk.Id,
                SourceDispenserStateId = sourceState.Id,
                ReplacementDispenserStateId = replacement.Id,
                SourceDeviceId = source.Id,
                ReplacementDeviceId = target.Id,
                SourceIngredientId = sourceState.IngredientId,
                ReplacementIngredientId = replacement.IngredientId,
                SourceContainerCode = sourceState.ContainerCode,
                ReplacementContainerCode = replacement.ContainerCode,
                EstimateDisposition = previousEstimate is > 0
                    ? InventoryEstimateDisposition.Transfer
                    : InventoryEstimateDisposition.None,
                PreviousEstimatedQuantity = previousEstimate,
                TransferredQuantity = transferredQuantity,
                SourceUnit = sourceState.Unit,
                ReplacementUnit = replacement.Unit,
                Reason = command.Request.Reason.Trim(),
                CreatedAt = now,
                CreatedByAccountId = command.UserContext.AccountId
            }, cancellationToken);
            replacements.Add(replacement);
        }

        source.Retire();
        source.DeletedAt = now;
        source.DeletedByAccountId = command.UserContext.AccountId;
        source.UpdatedAt = now;
        source.UpdatedByAccountId = command.UserContext.AccountId;
        await inventory.SaveChangesAsync(cancellationToken);

        return ApiResult<DeviceReplacementResult>.Success(new DeviceReplacementResult
        {
            SourceDeviceId = source.Id,
            ReplacementDeviceId = target.Id,
            ReboundContainerCount = replacements.Count,
            ReplacementDispenserStateIds = replacements.Select(state => state.Id).ToArray()
        }, "Device replaced and dispenser topology rebound.");
    }

}
