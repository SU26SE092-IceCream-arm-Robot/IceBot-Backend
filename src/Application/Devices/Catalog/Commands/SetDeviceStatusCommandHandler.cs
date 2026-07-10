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
using Application.Tenants.Kiosks;
using Domain.Devices.Catalog;

namespace Application.Devices.Catalog.Commands;

public sealed class SetDeviceStatusCommandHandler
{
    private readonly IDeviceManagementStore _deviceStore;

    public SetDeviceStatusCommandHandler(IDeviceManagementStore deviceStore)
    {
        _deviceStore = deviceStore;
    }

    public async Task<ApiResult<DeviceResult>> HandleAsync(
        SetDeviceStatusCommand command,
        CancellationToken cancellationToken = default)
    {
        var userContext = command.UserContext;
        var deviceId = command.DeviceId;
        var request = command.Request;

        var device = await _deviceStore.GetByKioskIdAsync(command.KioskId, deviceId, cancellationToken);
        if (device is null)
        {
            return ApiResult<DeviceResult>.Fail("Device not found.", 404);
        }

        if (device.Kiosk is null)
        {
            return ApiResult<DeviceResult>.Fail("Device kiosk is missing.", 400);
        }
        if (!KioskAccessRules.CanAccessKiosk(userContext, device.Kiosk))
        {
            return ApiResult<DeviceResult>.Fail("Access denied.", 403);
        }

        if (!Enum.IsDefined(request.Status))
        {
            return ApiResult<DeviceResult>.Fail("Invalid device status.", 400);
        }

        if (request.Status == DeviceStatus.Retired)
        {
            return ApiResult<DeviceResult>.Fail("Use the retire endpoint to retire a device.", 400);
        }

        if (device.Status == DeviceStatus.Retired)
        {
            return ApiResult<DeviceResult>.Fail("A retired device cannot change status.", 409);
        }

        device.SetStatus(request.Status);
        device.UpdatedAt = DateTimeOffset.UtcNow;
        device.UpdatedByAccountId = userContext.AccountId;

        await _deviceStore.SaveChangesAsync(cancellationToken);

        var updatedDevice = await _deviceStore.GetByKioskIdAsync(command.KioskId, deviceId, cancellationToken);
        if (updatedDevice is null)
        {
            return ApiResult<DeviceResult>.Fail("Device could not be retrieved after status update.", 500);
        }

        return ApiResult<DeviceResult>.Success(DeviceResultMapper.ToResult(updatedDevice), "Device status updated successfully.");
    }
}
