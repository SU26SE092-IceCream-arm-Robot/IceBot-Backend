using Application.Devices.Abstractions;
using Application.Devices.Mapping;
using Application.Devices.Results;
using Application.Shared.Wrappers;
using Application.Tenants.Kiosks;
using Domain.Devices.Enums;

namespace Application.Devices.Commands;

public sealed class RetireDeviceCommandHandler
{
    private readonly IDeviceManagementStore _deviceStore;

    public RetireDeviceCommandHandler(IDeviceManagementStore deviceStore)
    {
        _deviceStore = deviceStore;
    }

    public async Task<ApiResult<DeviceResult>> HandleAsync(
        RetireDeviceCommand command,
        CancellationToken cancellationToken = default)
    {
        var userContext = command.UserContext;
        var deviceId = command.DeviceId;

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

        var now = DateTimeOffset.UtcNow;
        device.Status = DeviceStatus.Retired;
        device.DeletedAt = now;
        device.DeletedByAccountId = userContext.AccountId;
        device.UpdatedAt = now;
        device.UpdatedByAccountId = userContext.AccountId;

        await _deviceStore.SaveChangesAsync(cancellationToken);

        return ApiResult<DeviceResult>.Success(DeviceResultMapper.ToResult(device), "Device retired and removed successfully.");
    }
}
