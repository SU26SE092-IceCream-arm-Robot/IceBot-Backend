using Application.Devices.Catalog.Abstractions;
using Application.Tenants;
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
using Application.Tenants.Kiosks;

namespace Application.Devices.Catalog.Queries;

public sealed class GetDeviceQueryHandler
{
    private readonly IDeviceManagementStore _deviceStore;

    public GetDeviceQueryHandler(IDeviceManagementStore deviceStore)
    {
        _deviceStore = deviceStore;
    }

    public async Task<ApiResult<DeviceResult>> HandleAsync(
        GetDeviceQuery query,
        CancellationToken cancellationToken = default)
    {
        var device = await _deviceStore.GetByKioskIdAsync(query.KioskId, query.DeviceId, cancellationToken);
        if (device is null)
        {
            return ApiResult<DeviceResult>.Fail("Device not found.", 404);
        }

        if (device.Kiosk is null)
        {
            return ApiResult<DeviceResult>.Fail("Device kiosk is missing.", 400);
        }
        if (!KioskAccessRules.CanAccessKiosk(ScopeRoleSets.DevicesView, query.UserContext, device.Kiosk))
        {
            return ApiResult<DeviceResult>.Fail("Access denied.", 403);
        }

        return ApiResult<DeviceResult>.Success(DeviceResultMapper.ToResult(device));
    }
}
