using Domain.Common;
using Domain.Inventory.Enums;

namespace Domain.Inventory.Entities;

public sealed class InventoryTopologyChangeRecord : AuditedEntity
{
    public Guid DispenserStateId { get; set; }
    public Guid KioskId { get; set; }
    public Guid DeviceId { get; set; }
    public Guid IngredientId { get; set; }
    public string ContainerCode { get; set; } = null!;
    public InventoryTopologyChangeType ChangeType { get; set; }
    public bool? BeforeIsActive { get; set; }
    public bool? AfterIsActive { get; set; }
    public decimal? BeforeCapacityQuantity { get; set; }
    public decimal? AfterCapacityQuantity { get; set; }
    public string? BeforeUnit { get; set; }
    public string? AfterUnit { get; set; }
    public string Reason { get; set; } = null!;
}
