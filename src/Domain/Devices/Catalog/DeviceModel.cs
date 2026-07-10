using Domain.Common;

namespace Domain.Devices.Catalog;

public partial class DeviceModel : BusinessEntity
{
    public long DeviceTypeId { get; set; }

    public string Code { get; set; } = null!;

    public string Name { get; set; } = null!;

    public string? Manufacturer { get; set; }

    public string? ModelNumber { get; set; }

    public string? FirmwareFamily { get; set; }

    public int CapabilitiesSchemaVersion { get; set; } = 1;

    public string? CapabilitiesJson { get; set; }

    public string? MetadataJson { get; set; }

    public virtual DeviceType DeviceType { get; set; } = null!;
}
