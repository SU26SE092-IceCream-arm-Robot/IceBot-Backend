using Domain.Common;
using Domain.Devices.Catalog;
using Domain.Inventory.Entities;
using Domain.Tenants.Entities;

namespace Domain.Devices.Catalog;

public partial class Device : BusinessEntity
{
    public long DeviceTypeId { get; private set; }

    public Guid? DeviceModelId { get; private set; }

    public Guid? KioskId { get; private set; }

    public string Code { get; private set; } = null!;

    public string Name { get; private set; } = null!;

    public string? SerialNumber { get; private set; }

    public DeviceStatus Status { get; private set; } = DeviceStatus.Provisioning;

    public string? PositionLabel { get; private set; }

    public string? FirmwareVersion { get; private set; }

    public DateTimeOffset? InstalledAt { get; private set; }

    public DateTimeOffset? LastSeenAt { get; set; }

    public string? MetadataJson { get; set; }

    public virtual DeviceModel? DeviceModel { get; set; }

    public virtual DeviceType DeviceType { get; set; } = null!;

    public virtual ICollection<IngredientDispenserState> IngredientDispenserStates { get; set; } = new List<IngredientDispenserState>();

    public virtual Kiosk? Kiosk { get; set; }

    private Device()
    {
    }

    public static Device CreateProvisioning(
        long deviceTypeId,
        Guid? deviceModelId,
        Guid kioskId,
        string code,
        string name,
        string? serialNumber,
        string? positionLabel,
        string? firmwareVersion,
        DateTimeOffset? installedAt)
    {
        if (deviceTypeId <= 0 || kioskId == Guid.Empty || string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(name))
        {
            throw new DomainRuleException("Device type, kiosk, code, and name are required.");
        }

        return new Device
        {
            DeviceTypeId = deviceTypeId,
            DeviceModelId = deviceModelId,
            KioskId = kioskId,
            Code = code.Trim(),
            Name = name.Trim(),
            SerialNumber = TrimToNull(serialNumber),
            Status = DeviceStatus.Provisioning,
            PositionLabel = TrimToNull(positionLabel),
            FirmwareVersion = TrimToNull(firmwareVersion),
            InstalledAt = installedAt
        };
    }

    public void UpdateDetails(
        long deviceTypeId,
        Guid? deviceModelId,
        string name,
        string? serialNumber,
        string? positionLabel,
        string? firmwareVersion,
        DateTimeOffset? installedAt)
    {
        if (Status == DeviceStatus.Retired)
        {
            throw new DomainRuleException("A retired device cannot be updated.");
        }

        if (deviceTypeId <= 0 || string.IsNullOrWhiteSpace(name))
        {
            throw new DomainRuleException("Device type and name are required.");
        }

        DeviceTypeId = deviceTypeId;
        DeviceModelId = deviceModelId;
        Name = name.Trim();
        SerialNumber = TrimToNull(serialNumber);
        PositionLabel = TrimToNull(positionLabel);
        FirmwareVersion = TrimToNull(firmwareVersion);
        InstalledAt = installedAt;
    }

    public void SetStatus(DeviceStatus status)
    {
        if (Status == DeviceStatus.Retired || status == DeviceStatus.Retired)
        {
            throw new DomainRuleException("Use the retirement workflow to retire a device.");
        }

        Status = status;
    }

    public void Retire()
    {
        Status = DeviceStatus.Retired;
    }

    private static string? TrimToNull(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
