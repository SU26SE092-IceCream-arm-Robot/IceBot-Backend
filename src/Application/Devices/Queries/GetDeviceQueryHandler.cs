using Application.Devices.Abstractions;
using Application.Devices.Mapping;
using Application.Devices.Results;
using Application.Shared.Wrappers;
using Application.Tenants.Kiosks;

namespace Application.Devices.Queries;

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
        var device = await _deviceStore.GetByIdAsync(query.DeviceId, cancellationToken);
        if (device is null)
        {
            return ApiResult<DeviceResult>.Fail("Device not found.", 404);
        }

        if (device.Kiosk is null)
        {
            return ApiResult<DeviceResult>.Fail("Device kiosk is missing.", 400);
        }

        if (!KioskAccessRules.CanAccessKiosk(query.UserContext, device.Kiosk))
        {
            return ApiResult<DeviceResult>.Fail("Access denied.", 403);
        }

        return ApiResult<DeviceResult>.Success(DeviceResultMapper.ToResult(device));
    }
}
