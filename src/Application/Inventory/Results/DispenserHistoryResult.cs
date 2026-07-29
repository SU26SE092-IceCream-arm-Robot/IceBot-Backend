namespace Application.Inventory.Results;

public sealed class DispenserHistoryResult
{
    public Guid EventId { get; set; }
    public string EventKind { get; set; } = null!;
    public string Action { get; set; } = null!;
    public Guid DispenserStateId { get; set; }
    public Guid? RelatedDispenserStateId { get; set; }
    public string? Reason { get; set; }
    public decimal? QuantityDelta { get; set; }
    public decimal? QuantityBefore { get; set; }
    public decimal? QuantityAfter { get; set; }
    public decimal? CapacityBefore { get; set; }
    public decimal? CapacityAfter { get; set; }
    public bool? ActiveBefore { get; set; }
    public bool? ActiveAfter { get; set; }
    public string? Unit { get; set; }
    public Guid? ActorAccountId { get; set; }
    public string ActorType { get; set; } = null!;
    public Guid? ActorReferenceId { get; set; }
    public string? ActorName { get; set; }
    public string? ActorEmail { get; set; }
    public DateTimeOffset OccurredAt { get; set; }
}
