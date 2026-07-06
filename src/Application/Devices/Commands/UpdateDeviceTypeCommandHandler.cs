using Application.Devices.Abstractions;
using Application.Devices.Mapping;
using Application.Devices.Results;
using Application.Shared.Wrappers;

namespace Application.Devices.Commands;

public sealed class UpdateDeviceTypeCommandHandler(IDeviceManagementStore store)
{
    public async Task<ApiResult<DeviceTypeResult>> HandleAsync(
        UpdateDeviceTypeCommand command,
        CancellationToken cancellationToken = default)
    {
        var entity = await store.GetDeviceTypeAsync(command.DeviceTypeId, false, cancellationToken);
        if (entity is null)
        {
            return ApiResult<DeviceTypeResult>.Fail("Device type not found.", 404);
        }

        var request = command.Request;
        entity.Name = request.Name.Trim();
        entity.Description = CreateDeviceTypeCommandHandler.TrimToNull(request.Description);
        entity.Category = request.Category.Trim();
        entity.RequiresKioskAssignment = request.RequiresKioskAssignment;
        entity.DisplayOrder = request.DisplayOrder;
        entity.UpdatedByAccountId = command.ActorId;
        await store.SaveChangesAsync(cancellationToken);
        return ApiResult<DeviceTypeResult>.Success(DeviceCatalogResultMapper.ToResult(entity), "Device type updated.");
    }
}
