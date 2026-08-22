using Application.Inventory.Abstractions;
using Application.Inventory.Mapping;
using Application.Inventory.Results;
using Application.Inventory.Support;
using Application.Shared.Wrappers;
using Application.Tenants;
using Domain.Common;
using Domain.Devices.Catalog;
using Domain.Inventory.Entities;
using Domain.Inventory.Enums;

namespace Application.Inventory.Commands;

public sealed class RebindDispenserStateCommandHandler(IInventoryStore inventory)
{
    public async Task<ApiResult<DispenserRebindResult>> HandleAsync(
        RebindDispenserStateCommand command,
        CancellationToken cancellationToken = default)
    {
        try
        {
            return await inventory.ExecuteInTransactionAsync(
                ct => RebindLockedAsync(command, ct), cancellationToken);
        }
        catch (DomainRuleException ex)
        {
            return ApiResult<DispenserRebindResult>.Fail(ex.Message, 409);
        }
    }

    private async Task<ApiResult<DispenserRebindResult>> RebindLockedAsync(
        RebindDispenserStateCommand command,
        CancellationToken cancellationToken)
    {
        await inventory.AcquireDeviceTopologyMutationLocksAsync(
            [command.Request.DeviceId], cancellationToken);
        await inventory.AcquireDispenserMutationLockAsync(command.DispenserStateId, cancellationToken);
        var source = await inventory.GetDispenserStateByIdAsync(command.DispenserStateId, cancellationToken);
        if (source?.Kiosk is null)
        {
            return ApiResult<DispenserRebindResult>.Fail("Dispenser state not found.", 404);
        }
        if (source.KioskId != command.KioskId)
        {
            return ApiResult<DispenserRebindResult>.Fail("Dispenser state not found.", 404);
        }

        if (!source.IsActive)
        {
            return ApiResult<DispenserRebindResult>.Fail("Only an active dispenser state can be rebound.", 409);
        }

        if (!ScopeAccessRules.CanAccessScopedRow(
                ScopeRoleSets.InventoryConfigure,
                command.UserContext,
                source.Kiosk.OrganizationId,
                source.Kiosk.StoreId,
                source.KioskId))
        {
            return ApiResult<DispenserRebindResult>.Fail("Access denied.", 403);
        }

        if (await inventory.HasActiveExecutionAsync(source.Kiosk.Id, cancellationToken))
        {
            return ApiResult<DispenserRebindResult>.Fail(
                "Inventory topology cannot be rebound while the kiosk has an accepted or running execution.", 409);
        }

        var request = command.Request;
        if (request.DeviceId == Guid.Empty || request.IngredientId == Guid.Empty)
        {
            return ApiResult<DispenserRebindResult>.Fail("Replacement device and ingredient are required.", 400);
        }

        var targetDevice = await inventory.GetDeviceForTopologyAsync(source.Kiosk.Id, request.DeviceId, cancellationToken);
        if (targetDevice is null)
        {
            return ApiResult<DispenserRebindResult>.Fail("Replacement device was not found in this kiosk.", 404);
        }
        if (targetDevice.Status == DeviceStatus.Retired)
        {
            return ApiResult<DispenserRebindResult>.Fail("Retired device cannot own a dispenser state.", 409);
        }

        var capabilityError = DispenserDeviceCapabilityRules.Validate(targetDevice.DeviceModel);
        if (capabilityError is not null)
        {
            return ApiResult<DispenserRebindResult>.Fail(capabilityError, 409);
        }

        var targetIngredient = await inventory.GetIngredientForTopologyAsync(request.IngredientId, cancellationToken);
        if (targetIngredient is null)
        {
            return ApiResult<DispenserRebindResult>.Fail("Replacement ingredient not found.", 404);
        }
        if (!targetIngredient.IsActive)
        {
            return ApiResult<DispenserRebindResult>.Fail("Inactive ingredient cannot be bound to a dispenser.", 409);
        }

        var profileError = DispenserLevelQuantityProfileContract.Validate(
            request.LevelToQuantityProfile,
            request.CapacityQuantity);
        if (profileError is not null)
        {
            return ApiResult<DispenserRebindResult>.Fail(profileError, 400);
        }

        var containerCode = request.ContainerCode.Trim().ToUpperInvariant();
        var unit = request.Unit.Trim();
        if (source.DeviceId == request.DeviceId &&
            source.IngredientId == request.IngredientId &&
            string.Equals(source.ContainerCode, containerCode, StringComparison.OrdinalIgnoreCase))
        {
            return ApiResult<DispenserRebindResult>.Fail(
                "Rebind must change device, ingredient, or container identity. Use update for configuration-only changes.", 400);
        }

        if (await inventory.DispenserIdentityExistsAsync(
                request.DeviceId,
                containerCode,
                source.Id,
                cancellationToken))
        {
            return ApiResult<DispenserRebindResult>.Fail(
                "Replacement container code already exists for the target device.", 409);
        }

        var estimateError = ValidateEstimateDisposition(source, request.IngredientId, unit, request.EstimateDisposition);
        if (estimateError is not null)
        {
            return ApiResult<DispenserRebindResult>.Fail(estimateError, 409);
        }

        var now = DateTimeOffset.UtcNow;
        var targetBalance = await inventory.GetKioskIngredientInventoryAsync(
            source.Kiosk.Id, targetIngredient.Id, unit, cancellationToken);
        if (targetBalance is null)
        {
            targetBalance = new KioskIngredientInventory
            {
                Id = Guid.NewGuid(),
                OrganizationId = source.Kiosk.OrganizationId,
                StoreId = source.Kiosk.StoreId,
                KioskId = source.Kiosk.Id,
                IngredientId = targetIngredient.Id,
                CreatedAt = now,
                CreatedByAccountId = command.UserContext.AccountId
            };
            targetBalance.Configure(unit, null, null, null, source.TrackingMode, now);
            await inventory.AddKioskIngredientInventoryAsync(targetBalance, cancellationToken);
        }
        var previousEstimate = source.EstimatedQuantity;
        var previousLevel = source.CurrentLevelStatus;
        var replacement = new IngredientDispenserState
        {
            DeviceId = targetDevice.Id,
            KioskId = source.KioskId,
            IngredientId = targetIngredient.Id,
            KioskIngredientInventoryId = targetBalance.Id,
            ContainerCode = containerCode,
            CurrentLevelStatus = IngredientLevelStatus.Unknown,
            LastMeasuredAt = now,
            IsActive = true,
            OriginNodeId = Guid.Empty,
            Version = 1,
            CreatedAt = now,
            CreatedByAccountId = command.UserContext.AccountId
        };
        replacement.ConfigureContainer(
            request.CapacityQuantity,
            unit,
            DispenserLevelQuantityProfileContract.Serialize(request.LevelToQuantityProfile));
        replacement.ChangeTrackingMode(source.TrackingMode);

        var transferredQuantity = 0m;
        if (previousEstimate is > 0)
        {
            source.AdjustEstimate(
                0,
                now,
                request.EstimateDisposition == InventoryEstimateDisposition.Transfer
                    ? "REBIND_TRANSFER_OUT"
                    : "REBIND_DISCARD",
                reportedLevelAfter: IngredientLevelStatus.Unknown);

            if (request.EstimateDisposition == InventoryEstimateDisposition.Transfer)
            {
                transferredQuantity = previousEstimate.Value;
                replacement.AdjustEstimate(
                    transferredQuantity,
                    now,
                    "REBIND_TRANSFER_IN",
                    reportedLevelAfter: previousLevel);
            }
        }

        source.Retire(command.UserContext.AccountId, now);
        await inventory.AddDispenserStateAsync(replacement, cancellationToken);
        await inventory.AddTopologyRebindRecordAsync(new InventoryTopologyRebindRecord
        {
            KioskId = source.Kiosk.Id,
            SourceDispenserStateId = source.Id,
            ReplacementDispenserStateId = replacement.Id,
            SourceDeviceId = source.DeviceId,
            ReplacementDeviceId = replacement.DeviceId,
            SourceIngredientId = source.IngredientId,
            ReplacementIngredientId = replacement.IngredientId,
            SourceContainerCode = source.ContainerCode,
            ReplacementContainerCode = replacement.ContainerCode,
            EstimateDisposition = request.EstimateDisposition,
            PreviousEstimatedQuantity = previousEstimate,
            TransferredQuantity = transferredQuantity,
            SourceUnit = source.Unit,
            ReplacementUnit = replacement.Unit,
            Reason = request.Reason.Trim(),
            CreatedAt = now,
            CreatedByAccountId = command.UserContext.AccountId
        }, cancellationToken);
        await inventory.SaveChangesAsync(cancellationToken);

        var created = await inventory.GetDispenserStateByIdAsync(replacement.Id, cancellationToken)
                      ?? throw new InvalidOperationException("Replacement dispenser state could not be reloaded.");
        return ApiResult<DispenserRebindResult>.Success(new DispenserRebindResult
        {
            SourceDispenserStateId = source.Id,
            Replacement = DispenserStateResultMapper.ToResult(created),
            EstimateDisposition = request.EstimateDisposition,
            PreviousEstimatedQuantity = previousEstimate,
            TransferredQuantity = transferredQuantity
        }, "Dispenser topology rebound.", 201);
    }

    private static string? ValidateEstimateDisposition(
        IngredientDispenserState source,
        Guid replacementIngredientId,
        string replacementUnit,
        InventoryEstimateDisposition disposition)
    {
        if (source.EstimatedQuantity is > 0)
        {
            if (disposition == InventoryEstimateDisposition.None)
            {
                return "Discard or Transfer must be selected when the source has an estimated quantity.";
            }
            if (disposition == InventoryEstimateDisposition.Transfer &&
                (source.IngredientId != replacementIngredientId ||
                 !string.Equals(source.Unit, replacementUnit, StringComparison.OrdinalIgnoreCase)))
            {
                return "Estimated quantity can be transferred only to the same ingredient and unit.";
            }
        }
        else if (disposition != InventoryEstimateDisposition.None)
        {
            return "Estimate disposition must be None when the source has no positive estimated quantity.";
        }

        return null;
    }

}
