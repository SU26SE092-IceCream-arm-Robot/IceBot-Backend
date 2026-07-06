using Application.Devices.Abstractions;
using Application.Devices.Mapping;
using Application.Devices.Results;
using Application.Shared.Wrappers;

namespace Application.Devices.Queries;

public sealed class GetDeviceModelQueryHandler(IDeviceManagementStore store)
{
    public async Task<ApiResult<DeviceModelResult>> HandleAsync(
        GetDeviceModelQuery query,
        CancellationToken cancellationToken = default)
    {
        var entity = await store.GetDeviceModelAsync(query.DeviceModelId, cancellationToken: cancellationToken);
        return entity is null
            ? ApiResult<DeviceModelResult>.Fail("Device model not found.", 404)
            : ApiResult<DeviceModelResult>.Success(DeviceCatalogResultMapper.ToResult(entity));
    }
}
