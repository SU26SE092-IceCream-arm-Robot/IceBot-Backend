using Application.Devices.Abstractions;
using Application.Devices.Mapping;
using Application.Devices.Results;
using Application.Shared.Wrappers;
using Application.Tenants.Kiosks;
using Domain.Devices.Enums;

namespace Application.Devices.Commands;

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

        var device = await _deviceStore.GetByIdAsync(deviceId, cancellationToken);
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

        device.Status = request.Status;
        device.UpdatedAt = DateTimeOffset.UtcNow;
        device.UpdatedByAccountId = userContext.AccountId;

        await _deviceStore.SaveChangesAsync(cancellationToken);

        var updatedDevice = await _deviceStore.GetByIdAsync(deviceId, cancellationToken);
        if (updatedDevice is null)
        {
            return ApiResult<DeviceResult>.Fail("Device could not be retrieved after status update.", 500);
        }

        return ApiResult<DeviceResult>.Success(DeviceResultMapper.ToResult(updatedDevice), "Device status updated successfully.");
    }
}
