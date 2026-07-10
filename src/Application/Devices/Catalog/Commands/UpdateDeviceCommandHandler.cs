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

namespace Application.Devices.Catalog.Commands;

public sealed class UpdateDeviceCommandHandler
{
    private readonly IDeviceManagementStore _deviceStore;

    public UpdateDeviceCommandHandler(IDeviceManagementStore deviceStore)
    {
        _deviceStore = deviceStore;
    }

    public async Task<ApiResult<DeviceResult>> HandleAsync(
        UpdateDeviceCommand command,
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

        if (device.Status == Domain.Devices.Catalog.DeviceStatus.Retired)
        {
            return ApiResult<DeviceResult>.Fail("A retired device cannot be updated.", 409);
        }
        if (!KioskAccessRules.CanAccessKiosk(userContext, device.Kiosk))
        {
            return ApiResult<DeviceResult>.Fail("Access denied.", 403);
        }

        var typeExists = await _deviceStore.DeviceTypeExistsAsync(request.DeviceTypeId, cancellationToken);
        if (!typeExists)
        {
            return ApiResult<DeviceResult>.Fail($"Device type with ID {request.DeviceTypeId} does not exist.", 400);
        }

        if (request.DeviceModelId.HasValue)
        {
            var modelExists = await _deviceStore.DeviceModelExistsForTypeAsync(request.DeviceModelId.Value, request.DeviceTypeId, cancellationToken);
            if (!modelExists)
            {
                return ApiResult<DeviceResult>.Fail($"Device model with ID {request.DeviceModelId.Value} does not exist or does not match device type {request.DeviceTypeId}.", 400);
            }
        }

        if (!string.IsNullOrWhiteSpace(request.SerialNumber))
        {
            var serial = request.SerialNumber.Trim();
            var serialExists = await _deviceStore.SerialNumberExistsAsync(serial, deviceId, cancellationToken);
            if (serialExists)
            {
                return ApiResult<DeviceResult>.Fail($"Device with serial number '{serial}' already exists.", 409);
            }
        }

        device.UpdateDetails(
            request.DeviceTypeId,
            request.DeviceModelId,
            request.Name,
            request.SerialNumber,
            request.PositionLabel,
            request.FirmwareVersion,
            request.InstalledAt);
        device.UpdatedAt = DateTimeOffset.UtcNow;
        device.UpdatedByAccountId = userContext.AccountId;

        await _deviceStore.SaveChangesAsync(cancellationToken);

        var updatedDevice = await _deviceStore.GetByKioskIdAsync(command.KioskId, deviceId, cancellationToken);
        if (updatedDevice is null)
        {
            return ApiResult<DeviceResult>.Fail("Device could not be retrieved after update.", 500);
        }

        return ApiResult<DeviceResult>.Success(DeviceResultMapper.ToResult(updatedDevice), "Device updated successfully.");
    }
}
