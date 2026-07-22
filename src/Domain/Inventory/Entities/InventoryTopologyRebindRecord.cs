using Domain.Common;
using Domain.Inventory.Enums;

namespace Domain.Inventory.Entities;

public sealed class InventoryTopologyRebindRecord : AuditedEntity
{
    public Guid KioskId { get; set; }
    public Guid SourceDispenserStateId { get; set; }
    public Guid ReplacementDispenserStateId { get; set; }
    public Guid SourceDeviceId { get; set; }
    public Guid ReplacementDeviceId { get; set; }
    public Guid SourceIngredientId { get; set; }
    public Guid ReplacementIngredientId { get; set; }
    public string SourceContainerCode { get; set; } = null!;
    public string ReplacementContainerCode { get; set; } = null!;
    public InventoryEstimateDisposition EstimateDisposition { get; set; }
    public decimal? PreviousEstimatedQuantity { get; set; }
    public decimal TransferredQuantity { get; set; }
    public string SourceUnit { get; set; } = null!;
    public string ReplacementUnit { get; set; } = null!;
    public string Reason { get; set; } = null!;
}
