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

namespace Application.Devices.Catalog.Queries;

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
