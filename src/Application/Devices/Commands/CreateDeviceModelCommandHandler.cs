using Application.Devices.Abstractions;
using Application.Devices.Mapping;
using Application.Devices.Results;
using Application.Devices.Support;
using Application.Shared.Wrappers;
using Domain.Devices.Entities;

namespace Application.Devices.Commands;

public sealed class CreateDeviceModelCommandHandler(IDeviceManagementStore store)
{
    public async Task<ApiResult<DeviceModelResult>> HandleAsync(
        CreateDeviceModelCommand command,
        CancellationToken cancellationToken = default)
    {
        var type = await store.GetDeviceTypeAsync(command.DeviceTypeId, cancellationToken: cancellationToken);
        if (type is null)
        {
            return ApiResult<DeviceModelResult>.Fail("Device type not found.", 404);
        }
        if (!type.IsActive)
        {
            return ApiResult<DeviceModelResult>.Fail("Device type is inactive.", 409);
        }

        var request = command.Request;
        var validationError = DeviceModelRequestValidator.ValidateCapabilities(request.Capabilities);
        if (validationError is not null)
        {
            return ApiResult<DeviceModelResult>.Fail(validationError, 400);
        }

        var code = CreateDeviceTypeCommandHandler.NormalizeCode(request.Code);
        if (await store.DeviceModelCodeExistsAsync(command.DeviceTypeId, code, cancellationToken: cancellationToken))
        {
            return ApiResult<DeviceModelResult>.Fail("Device model code already exists for this device type.", 409);
        }

        var entity = new DeviceModel
        {
            Id = Guid.NewGuid(),
            DeviceTypeId = command.DeviceTypeId,
            Code = code,
            Name = request.Name.Trim(),
            Manufacturer = CreateDeviceTypeCommandHandler.TrimToNull(request.Manufacturer),
            ModelNumber = CreateDeviceTypeCommandHandler.TrimToNull(request.ModelNumber),
            FirmwareFamily = CreateDeviceTypeCommandHandler.TrimToNull(request.FirmwareFamily),
            CapabilitiesSchemaVersion = 1,
            CapabilitiesJson = DeviceCatalogResultMapper.SerializeCapabilities(request.Capabilities),
            CreatedByAccountId = command.ActorId
        };
        await store.AddDeviceModelAsync(entity, cancellationToken);
        await store.SaveChangesAsync(cancellationToken);
        return ApiResult<DeviceModelResult>.Success(DeviceCatalogResultMapper.ToResult(entity), "Device model created.", 201);
    }
}
