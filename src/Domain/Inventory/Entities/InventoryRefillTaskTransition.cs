using Domain.Common;
using Domain.Inventory.Enums;

namespace Domain.Inventory.Entities;

public sealed class InventoryRefillTaskTransition : AppendOnlySyncEntity
{
    public Guid InventoryRefillTaskId { get; set; }
    public InventoryRefillTaskStatus? FromStatus { get; set; }
    public InventoryRefillTaskStatus ToStatus { get; set; }
    public Guid? ActorAccountId { get; set; }
    public string? ActorRoleCode { get; set; }
    public Guid? ActorOrganizationId { get; set; }
    public Guid? ActorStoreId { get; set; }
    public Guid? ActorKioskId { get; set; }
    public string? Reason { get; set; }
    public decimal? ActualQuantity { get; set; }
    public string RequestIdempotencyKey { get; set; } = null!;
    public string RequestFingerprint { get; set; } = null!;
    public DateTimeOffset OccurredAt { get; set; }
}
