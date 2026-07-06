using Application.Devices.Abstractions;
using Application.Devices.Mapping;
using Application.Devices.Results;
using Application.Shared.Wrappers;

namespace Application.Devices.Queries;

public sealed class ListDeviceModelsQueryHandler(IDeviceManagementStore store)
{
    public async Task<ApiResult<IReadOnlyList<DeviceModelResult>>> HandleAsync(
        ListDeviceModelsQuery query,
        CancellationToken cancellationToken = default)
    {
        if (await store.GetDeviceTypeAsync(query.DeviceTypeId, cancellationToken: cancellationToken) is null)
        {
            return ApiResult<IReadOnlyList<DeviceModelResult>>.Fail("Device type not found.", 404);
        }

        var entities = await store.ListDeviceModelsAsync(query.DeviceTypeId, query.Search, cancellationToken);
        return ApiResult<IReadOnlyList<DeviceModelResult>>.Success(
            entities.Select(DeviceCatalogResultMapper.ToResult).ToList());
    }
}
