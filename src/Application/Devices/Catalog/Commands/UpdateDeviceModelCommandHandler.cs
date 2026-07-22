using Application.Devices.Catalog.Abstractions;
using Application.Devices.ExecutionEndpoints.Abstractions;
using Application.Devices.Telemetry.Abstractions;
using Application.Devices.Connectivity.Abstractions;
using Application.Devices.Credentials.Abstractions;
using Application.Devices.Catalog.Mapping;
using Application.Devices.ExecutionEndpoints.Mapping;
using Application.Devices.Telemetry.Mapping;
using Application.Devices.Catalog.Results;
using Application.Devices.ExecutionEndpoints.Results;
using Application.Devices.Telemetry.Results;
using Application.Devices.Connectivity.Results;
using Application.Devices.Credentials.Results;
using Application.Devices.Catalog.Support;
using Application.Shared.Wrappers;

namespace Application.Devices.Catalog.Commands;

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

        var currentCapabilities = Application.Devices.Catalog.Support.DeviceCapabilityContract.Deserialize(entity.CapabilitiesJson);
        var removesDispenserCapability =
            currentCapabilities.Contains(Application.Devices.Catalog.Support.DeviceCapabilityContract.IngredientDispenser, StringComparer.OrdinalIgnoreCase) &&
            !request.Capabilities.Contains(Application.Devices.Catalog.Support.DeviceCapabilityContract.IngredientDispenser, StringComparer.OrdinalIgnoreCase);
        if (removesDispenserCapability &&
            await store.DeviceModelHasActiveDispenserStatesAsync(entity.Id, cancellationToken))
        {
            return ApiResult<DeviceModelResult>.Fail(
                "IngredientDispenser capability cannot be removed while active dispenser bindings use this model.", 409);
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
