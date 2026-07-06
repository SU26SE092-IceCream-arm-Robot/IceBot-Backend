using Application.Devices.Abstractions;
using Application.Devices.Mapping;
using Application.Devices.Results;
using Application.Shared.Wrappers;

namespace Application.Devices.Commands;

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
