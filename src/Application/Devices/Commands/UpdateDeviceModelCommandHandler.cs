using Application.Devices.Abstractions;
using Application.Devices.Mapping;
using Application.Devices.Results;
using Application.Devices.Support;
using Application.Shared.Wrappers;

namespace Application.Devices.Commands;

public sealed class UpdateDeviceModelCommandHandler(IDeviceManagementStore store)
{
    public async Task<ApiResult<DeviceModelResult>> HandleAsync(
        UpdateDeviceModelCommand command,
        CancellationToken cancellationToken = default)
    {
        var entity = await store.GetDeviceModelAsync(command.DeviceModelId, false, cancellationToken);
        if (entity is null)
        {
            return ApiResult<DeviceModelResult>.Fail("Device model not found.", 404);
        }

        var request = command.Request;
        var validationError = DeviceModelRequestValidator.ValidateCapabilities(request.Capabilities);
        if (validationError is not null)
        {
            return ApiResult<DeviceModelResult>.Fail(validationError, 400);
        }

        var currentCapabilities = Application.Devices.Support.DeviceCapabilityContract.Deserialize(entity.CapabilitiesJson);
        var removesDispenserCapability =
            currentCapabilities.Contains(Application.Devices.Support.DeviceCapabilityContract.IngredientDispenser, StringComparer.OrdinalIgnoreCase) &&
            !request.Capabilities.Contains(Application.Devices.Support.DeviceCapabilityContract.IngredientDispenser, StringComparer.OrdinalIgnoreCase);
        if (removesDispenserCapability &&
            await store.DeviceModelHasActiveDispenserStatesAsync(entity.Id, false, cancellationToken))
        {
            return ApiResult<DeviceModelResult>.Fail(
                "IngredientDispenser capability cannot be removed while active dispenser bindings use this model.", 409);
        }

        var removesLevelSensorCapability =
            currentCapabilities.Contains(Application.Devices.Support.DeviceCapabilityContract.LevelSensor, StringComparer.OrdinalIgnoreCase) &&
            !request.Capabilities.Contains(Application.Devices.Support.DeviceCapabilityContract.LevelSensor, StringComparer.OrdinalIgnoreCase);
        if (removesLevelSensorCapability &&
            await store.DeviceModelHasActiveDispenserStatesAsync(entity.Id, true, cancellationToken))
        {
            return ApiResult<DeviceModelResult>.Fail(
                "LevelSensor capability cannot be removed while active level-profile bindings use this model.", 409);
        }

        entity.Name = request.Name.Trim();
        entity.Manufacturer = CreateDeviceTypeCommandHandler.TrimToNull(request.Manufacturer);
        entity.ModelNumber = CreateDeviceTypeCommandHandler.TrimToNull(request.ModelNumber);
        entity.FirmwareFamily = CreateDeviceTypeCommandHandler.TrimToNull(request.FirmwareFamily);
        entity.CapabilitiesJson = DeviceCatalogResultMapper.SerializeCapabilities(request.Capabilities);
        entity.UpdatedByAccountId = command.ActorId;
        await store.SaveChangesAsync(cancellationToken);
        return ApiResult<DeviceModelResult>.Success(DeviceCatalogResultMapper.ToResult(entity), "Device model updated.");
    }
}
