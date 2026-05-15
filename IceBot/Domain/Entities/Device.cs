using Domain.Common;
using Domain.Enums;

namespace Domain.Entities;

public partial class Device : BusinessEntity
{
    public long DeviceTypeId { get; set; }

    public Guid? DeviceModelId { get; set; }

    public Guid? KioskId { get; set; }

    public string Code { get; set; } = null!;

    public string Name { get; set; } = null!;

    public string? SerialNumber { get; set; }

    public DeviceStatus Status { get; set; } = DeviceStatus.Provisioning;

    public string? PositionLabel { get; set; }

    public string? FirmwareVersion { get; set; }

    public DateTimeOffset? InstalledAt { get; set; }

    public DateTimeOffset? LastSeenAt { get; set; }

    public string? MetadataJson { get; set; }

    public virtual DeviceModel? DeviceModel { get; set; }

    public virtual DeviceType DeviceType { get; set; } = null!;

    public virtual ICollection<IngredientDispenserState> IngredientDispenserStates { get; set; } = new List<IngredientDispenserState>();

    public virtual Kiosk? Kiosk { get; set; }
}
