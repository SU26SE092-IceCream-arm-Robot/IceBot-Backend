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
using Application.Shared.Wrappers;

namespace Application.Devices.Catalog.Commands;

public sealed class SetDeviceTypeStatusCommandHandler(IDeviceManagementStore store)
{
    public async Task<ApiResult<DeviceTypeResult>> HandleAsync(
        SetDeviceTypeStatusCommand command,
        CancellationToken cancellationToken = default)
    {
        var entity = await store.GetDeviceTypeAsync(command.DeviceTypeId, false, cancellationToken);
        if (entity is null)
        {
            return ApiResult<DeviceTypeResult>.Fail("Device type not found.", 404);
        }

        entity.IsActive = command.IsActive;
        entity.UpdatedByAccountId = command.ActorId;
        await store.SaveChangesAsync(cancellationToken);
        return ApiResult<DeviceTypeResult>.Success(DeviceCatalogResultMapper.ToResult(entity), "Device type status updated.");
    }
}
