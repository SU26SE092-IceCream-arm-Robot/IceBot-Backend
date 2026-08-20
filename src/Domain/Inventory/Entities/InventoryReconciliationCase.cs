using Domain.Common;
using Domain.Inventory.Enums;

namespace Domain.Inventory.Entities;

/// <summary>Operator work created when physical production cannot be fully reflected in the estimated balance.</summary>
public sealed class InventoryReconciliationCase : SyncAggregateEntity
{
    public Guid OrganizationId { get; set; }
    public Guid StoreId { get; set; }
    public Guid KioskId { get; set; }
    public Guid IngredientId { get; set; }
    public Guid? KioskIngredientInventoryId { get; set; }
    public Guid SourceEventId { get; set; }
    public decimal ExpectedQuantity { get; set; }
    public decimal AppliedQuantity { get; set; }
    public string Unit { get; set; } = "gram";
    public string ReasonCode { get; set; } = null!;
    public InventoryReconciliationCaseStatus Status { get; set; } = InventoryReconciliationCaseStatus.Open;
    public DateTimeOffset? ResolvedAt { get; set; }
    public Guid? ResolvedByAccountId { get; set; }
    public string? ResolutionNote { get; set; }
}
