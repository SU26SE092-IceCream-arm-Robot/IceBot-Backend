using Application.Devices.Results;
using Application.Devices.Support;
using Domain.Devices.Entities;

namespace Application.Devices.Mapping;

public static class DeviceCatalogResultMapper
{
    public static DeviceTypeResult ToResult(DeviceType entity) => new(
        entity.Id,
        entity.Code,
        entity.Name,
        entity.Description,
        entity.Category,
        entity.RequiresKioskAssignment,
        entity.IsActive,
        entity.DisplayOrder);

    public static DeviceModelResult ToResult(DeviceModel entity) => new(
        entity.Id,
        entity.DeviceTypeId,
        entity.Code,
        entity.Name,
        entity.Manufacturer,
        entity.ModelNumber,
        entity.FirmwareFamily,
        DeviceCapabilityContract.Deserialize(entity.CapabilitiesJson));

    public static string? SerializeCapabilities(IReadOnlyList<string>? capabilities)
    {
        return DeviceCapabilityContract.Serialize(capabilities);
    }
}
