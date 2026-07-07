using Application.Devices.Abstractions;
using Application.Devices.Mapping;
using Application.Devices.Results;
using Application.Shared.Wrappers;
using Application.Tenants.Kiosks;
using Domain.Devices.Enums;
using Application.Inventory.Abstractions;
using Application.Inventory.Support;
using Domain.Inventory.Enums;

namespace Application.Devices.Commands;

public sealed class RetireDeviceCommandHandler
{
    private readonly IDeviceManagementStore _deviceStore;
    private readonly IInventoryStore _inventory;

    public RetireDeviceCommandHandler(IDeviceManagementStore deviceStore, IInventoryStore inventory)
    {
        _deviceStore = deviceStore;
        _inventory = inventory;
    }

    public async Task<ApiResult<DeviceResult>> HandleAsync(
        RetireDeviceCommand command,
        CancellationToken cancellationToken = default)
    {
        var userContext = command.UserContext;
        var deviceId = command.DeviceId;

        if (command.Reason?.Trim().Length > 500)
        {
            return ApiResult<DeviceResult>.Fail("Retirement reason must be at most 500 characters.", 400);
        }

        return await _inventory.ExecuteInTransactionAsync(async ct =>
        {
        var device = await _deviceStore.GetByIdAsync(deviceId, ct);
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

        if (await _inventory.HasActiveExecutionAsync(device.Kiosk.Id, ct))
        {
            return ApiResult<DeviceResult>.Fail(
                "Device cannot be retired while its kiosk has an accepted or running execution.", 409);
        }

        var now = DateTimeOffset.UtcNow;
        var reason = string.IsNullOrWhiteSpace(command.Reason) ? "DEVICE_RETIRED" : command.Reason.Trim();
        var states = await _inventory.ListActiveDispenserStatesByDeviceAsync(device.Id, ct);
        foreach (var state in states)
        {
            state.Retire(userContext.AccountId, now);
            await _inventory.AddTopologyChangeRecordAsync(
                InventoryTopologyAuditFactory.Create(
                    state,
                    InventoryTopologyChangeType.Retired,
                    reason,
                    userContext.AccountId,
                    now,
                    true,
                    state.CapacityQuantity,
                    state.Unit),
                ct);
        }

        device.Status = DeviceStatus.Retired;
        device.DeletedAt = now;
        device.DeletedByAccountId = userContext.AccountId;
        device.UpdatedAt = now;
        device.UpdatedByAccountId = userContext.AccountId;

        await _inventory.SaveChangesAsync(ct);

        return ApiResult<DeviceResult>.Success(DeviceResultMapper.ToResult(device), "Device and its dispenser topology retired successfully.");
        }, cancellationToken);
    }
}
