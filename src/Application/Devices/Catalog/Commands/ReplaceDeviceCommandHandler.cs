using Application.Devices.Catalog.Abstractions;
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
        if (!KioskAccessRules.CanAccessKiosk(command.UserContext, source.Kiosk))
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

        var sourceStates = await inventory.ListActiveDispenserStatesByDeviceAsync(source.Id, cancellationToken);
        if (sourceStates.Count > 0)
        {
            var capabilityError = DispenserDeviceCapabilityRules.Validate(target.DeviceModel);
            if (capabilityError is not null)
            {
                return ApiResult<DeviceReplacementResult>.Fail(capabilityError, 409);
            }
        }

        await inventory.AcquireDispenserMutationLockAsync(target.Id, cancellationToken);
        foreach (var sourceState in sourceStates.OrderBy(state => state.Id))
        {
            await inventory.AcquireDispenserMutationLockAsync(sourceState.Id, cancellationToken);
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

            var transferredQuantity = 0m;
            if (previousEstimate is > 0)
            {
                var transferOut = sourceState.AdjustEstimate(
                    0,
                    now,
                    "DEVICE_REPLACEMENT_TRANSFER_OUT",
                    reportedLevelAfter: IngredientLevelStatus.Unknown);
                EnrichMovement(transferOut, sourceState, command.UserContext.AccountId, now, replacement.Id);
                await inventory.AddStockMovementAsync(transferOut, cancellationToken);

                transferredQuantity = previousEstimate.Value;
                var transferIn = replacement.AdjustEstimate(
                    transferredQuantity,
                    now,
                    "DEVICE_REPLACEMENT_TRANSFER_IN",
                    reportedLevelAfter: previousLevel);
                EnrichMovement(transferIn, replacement, command.UserContext.AccountId, now, sourceState.Id);
                transferIn.OrganizationId = source.Kiosk.OrganizationId;
                transferIn.StoreId = source.Kiosk.StoreId;
                await inventory.AddStockMovementAsync(transferIn, cancellationToken);
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

    private static void EnrichMovement(
        StockMovement movement,
        IngredientDispenserState state,
        Guid actorId,
        DateTimeOffset now,
        Guid relatedStateId)
    {
        movement.OrganizationId = state.Kiosk?.OrganizationId;
        movement.StoreId = state.Kiosk?.StoreId;
        movement.ReferenceType = "DeviceReplacement";
        movement.ReferenceId = relatedStateId;
        movement.CreatedAt = now;
        movement.CreatedByAccountId = actorId;
    }
}
