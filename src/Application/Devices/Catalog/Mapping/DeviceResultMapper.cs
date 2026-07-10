using Application.Devices.Catalog.Results;
using Application.Devices.ExecutionEndpoints.Results;
using Application.Devices.Telemetry.Results;
using Application.Devices.Connectivity.Results;
using Application.Devices.Credentials.Results;
using Domain.Devices.Catalog;

namespace Application.Devices.Catalog.Mapping;

internal static class DeviceResultMapper
{
    public static DeviceResult ToResult(Device device)
    {
        return new DeviceResult
        {
            Id = device.Id,
            KioskId = device.KioskId,
            KioskCode = device.Kiosk?.Code,
            StoreId = device.Kiosk?.StoreId,
            OrganizationId = device.Kiosk?.OrganizationId,
            DeviceTypeId = device.DeviceTypeId,
            DeviceTypeCode = device.DeviceType?.Code ?? string.Empty,
            DeviceModelId = device.DeviceModelId,
            DeviceModelCode = device.DeviceModel?.Code,
            Code = device.Code,
            Name = device.Name,
            SerialNumber = device.SerialNumber,
            Status = device.Status.ToString(),
            PositionLabel = device.PositionLabel,
            FirmwareVersion = device.FirmwareVersion,
            InstalledAt = device.InstalledAt,
            LastSeenAt = device.LastSeenAt,
            CreatedAt = device.CreatedAt,
            UpdatedAt = device.UpdatedAt
        };
    }
}
