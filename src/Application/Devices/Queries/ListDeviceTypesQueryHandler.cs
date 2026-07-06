using Application.Devices.Abstractions;
using Application.Devices.Mapping;
using Application.Devices.Results;
using Application.Shared.Wrappers;

namespace Application.Devices.Queries;

public sealed class ListDeviceTypesQueryHandler(IDeviceManagementStore store)
{
    public async Task<ApiResult<IReadOnlyList<DeviceTypeResult>>> HandleAsync(
        ListDeviceTypesQuery query,
        CancellationToken cancellationToken = default)
    {
        var entities = await store.ListDeviceTypesAsync(query.Search, query.IsActive, cancellationToken);
        return ApiResult<IReadOnlyList<DeviceTypeResult>>.Success(
            entities.Select(DeviceCatalogResultMapper.ToResult).ToList());
    }
}
