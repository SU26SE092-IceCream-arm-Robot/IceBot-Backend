using Domain.Devices.Enums;
using Domain.Inventory.Enums;

namespace Application.Inventory.Results;

public sealed class KioskInventoryTopologyResult
{
    public Guid KioskId { get; set; }
    public string KioskCode { get; set; } = null!;
    public string KioskName { get; set; } = null!;
    public IReadOnlyList<InventoryTopologyDeviceResult> Devices { get; set; } = [];
}

public sealed class InventoryTopologyDeviceResult
{
    public Guid DeviceId { get; set; }
    public string Code { get; set; } = null!;
    public string Name { get; set; } = null!;
    public DeviceStatus Status { get; set; }
    public long DeviceTypeId { get; set; }
    public string DeviceTypeCode { get; set; } = null!;
    public Guid? DeviceModelId { get; set; }
    public string? DeviceModelCode { get; set; }
    public IReadOnlyList<string> Capabilities { get; set; } = [];
    public bool CanHostDispenser { get; set; }
    public bool HasConfiguredContainers { get; set; }
    public IReadOnlyList<InventoryTopologyContainerResult> Containers { get; set; } = [];
}

public sealed class InventoryTopologyContainerResult
{
    public Guid DispenserStateId { get; set; }
    public string ContainerCode { get; set; } = null!;
    public Guid IngredientId { get; set; }
    public string IngredientCode { get; set; } = null!;
    public string IngredientName { get; set; } = null!;
    public IngredientLevelStatus CurrentLevelStatus { get; set; }
    public decimal? EstimatedQuantity { get; set; }
    public decimal? CapacityQuantity { get; set; }
    public string Unit { get; set; } = null!;
    public bool IsActive { get; set; }
}
