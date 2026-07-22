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
