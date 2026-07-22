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
