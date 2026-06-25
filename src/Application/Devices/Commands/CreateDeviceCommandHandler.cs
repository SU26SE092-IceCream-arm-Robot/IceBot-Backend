using Application.Devices.Abstractions;
using Application.Devices.Mapping;
using Application.Devices.Results;
using Application.Shared.Wrappers;
using Application.Tenants.Kiosks;
using Domain.Devices.Entities;
using Domain.Devices.Enums;
using System.Text.Json;

namespace Application.Devices.Commands;

public sealed class CreateDeviceCommandHandler
{
    private readonly IDeviceManagementStore _deviceStore;

    public CreateDeviceCommandHandler(IDeviceManagementStore deviceStore)
    {
        _deviceStore = deviceStore;
    }

    public async Task<ApiResult<DeviceResult>> HandleAsync(
        CreateDeviceCommand command,
        CancellationToken cancellationToken = default)
    {
        var userContext = command.UserContext;
        var kioskId = command.KioskId;
        var request = command.Request;

        var kiosk = await _deviceStore.GetKioskByIdAsync(kioskId, cancellationToken);
        if (kiosk is null)
        {
            return ApiResult<DeviceResult>.Fail("Kiosk not found.", 404);
        }

        if (!KioskAccessRules.CanAccessKiosk(userContext, kiosk))
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

        var code = request.Code.Trim();
        var codeExists = await _deviceStore.CodeExistsInKioskAsync(kioskId, code, cancellationToken: cancellationToken);
        if (codeExists)
        {
            return ApiResult<DeviceResult>.Fail($"Device with code '{code}' already exists in this kiosk.", 409);
        }

        if (!string.IsNullOrWhiteSpace(request.SerialNumber))
        {
            var serial = request.SerialNumber.Trim();
            var serialExists = await _deviceStore.SerialNumberExistsAsync(serial, cancellationToken: cancellationToken);
            if (serialExists)
            {
                return ApiResult<DeviceResult>.Fail($"Device with serial number '{serial}' already exists.", 409);
            }
        }

        if (!string.IsNullOrWhiteSpace(request.MetadataJson) && !IsValidJson(request.MetadataJson))
        {
            return ApiResult<DeviceResult>.Fail("MetadataJson must be a valid JSON string.", 400);
        }

        var device = new Device
        {
            Id = Guid.NewGuid(),
            DeviceTypeId = request.DeviceTypeId,
            DeviceModelId = request.DeviceModelId,
            KioskId = kioskId,
            Code = code,
            Name = request.Name.Trim(),
            SerialNumber = request.SerialNumber?.Trim(),
            Status = DeviceStatus.Provisioning,
            PositionLabel = request.PositionLabel?.Trim(),
            FirmwareVersion = request.FirmwareVersion?.Trim(),
            InstalledAt = request.InstalledAt,
            MetadataJson = request.MetadataJson?.Trim(),
            CreatedAt = DateTimeOffset.UtcNow,
            CreatedByAccountId = userContext.AccountId
        };

        await _deviceStore.AddAsync(device, cancellationToken);
        await _deviceStore.SaveChangesAsync(cancellationToken);

        // Fetch fully populated entity for mapping (includes Kiosk, DeviceType, DeviceModel)
        var createdDevice = await _deviceStore.GetByIdAsync(device.Id, cancellationToken);
        if (createdDevice is null)
        {
            return ApiResult<DeviceResult>.Fail("Device could not be retrieved after creation.", 500);
        }

        return ApiResult<DeviceResult>.Success(DeviceResultMapper.ToResult(createdDevice), "Device created successfully.", 201);
    }

    private static bool IsValidJson(string json)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            return true;
        }
        catch (JsonException)
        {
            return false;
        }
    }
}
