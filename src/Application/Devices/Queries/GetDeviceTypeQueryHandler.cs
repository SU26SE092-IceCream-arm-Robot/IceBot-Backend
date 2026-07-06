using Application.Devices.Abstractions;
using Application.Devices.Mapping;
using Application.Devices.Results;
using Application.Shared.Wrappers;

namespace Application.Devices.Queries;

public sealed class GetDeviceTypeQueryHandler(IDeviceManagementStore store)
{
    public async Task<ApiResult<DeviceTypeResult>> HandleAsync(
        GetDeviceTypeQuery query,
        CancellationToken cancellationToken = default)
    {
        var entity = await store.GetDeviceTypeAsync(query.DeviceTypeId, cancellationToken: cancellationToken);
        return entity is null
            ? ApiResult<DeviceTypeResult>.Fail("Device type not found.", 404)
            : ApiResult<DeviceTypeResult>.Success(DeviceCatalogResultMapper.ToResult(entity));
    }
}
