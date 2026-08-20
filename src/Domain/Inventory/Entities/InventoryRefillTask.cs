using Domain.Common;
using Domain.Inventory.Enums;

namespace Domain.Inventory.Entities;

public sealed class InventoryRefillTask : SyncAggregateEntity
{
    public Guid OrganizationId { get; set; }
    public Guid StoreId { get; set; }
    public Guid KioskId { get; set; }
    public Guid KioskIngredientInventoryId { get; set; }
    public Guid? IngredientDispenserStateId { get; set; }
    public Guid? SourceAlertId { get; set; }
    public InventoryRefillRequestSource RequestSource { get; set; }
    public InventoryRefillTaskStatus Status { get; private set; } = InventoryRefillTaskStatus.Requested;
    public decimal? RequestedQuantity { get; set; }
    public decimal? ActualQuantity { get; private set; }
    public string Unit { get; set; } = "gram";
    public string? ReasonCode { get; private set; }
    public string? Notes { get; private set; }
    public string? ExternalLotReference { get; private set; }
    public DateTimeOffset RequestedAt { get; set; }
    public Guid? RequestedByAccountId { get; set; }
    public DateTimeOffset? StartedAt { get; private set; }
    public Guid? StartedByAccountId { get; private set; }
    public DateTimeOffset? CompletedAt { get; private set; }
    public Guid? CompletedByAccountId { get; private set; }
    public DateTimeOffset? CancelledAt { get; private set; }
    public Guid? CancelledByAccountId { get; private set; }
    public string? CancellationReason { get; private set; }
    public string RequestIdempotencyKey { get; set; } = null!;
    public string RequestFingerprint { get; set; } = null!;

    public void Start(Guid actorAccountId, DateTimeOffset now)
    {
        if (Status != InventoryRefillTaskStatus.Requested) throw new DomainRuleException("Only a requested refill task can be started.");
        Status = InventoryRefillTaskStatus.InProgress;
        StartedAt = now;
        StartedByAccountId = actorAccountId;
        Version++;
    }

    public void Complete(decimal actualQuantity, string? reasonCode, string? notes, string? externalLotReference, Guid actorAccountId, DateTimeOffset now)
    {
        if (Status is not (InventoryRefillTaskStatus.Requested or InventoryRefillTaskStatus.InProgress)) throw new DomainRuleException("Only an active refill task can be completed.");
        if (actualQuantity <= 0) throw new DomainRuleException("Actual refill quantity must be greater than zero.");
        Status = InventoryRefillTaskStatus.Completed;
        ActualQuantity = actualQuantity;
        ReasonCode = Normalize(reasonCode);
        Notes = Normalize(notes);
        ExternalLotReference = Normalize(externalLotReference);
        CompletedAt = now;
        CompletedByAccountId = actorAccountId;
        Version++;
    }

    public void Cancel(string reason, Guid actorAccountId, DateTimeOffset now)
    {
        if (Status is InventoryRefillTaskStatus.Completed or InventoryRefillTaskStatus.Cancelled) throw new DomainRuleException("A terminal refill task cannot be cancelled.");
        if (string.IsNullOrWhiteSpace(reason)) throw new DomainRuleException("Cancellation reason is required.");
        Status = InventoryRefillTaskStatus.Cancelled;
        CancellationReason = reason.Trim();
        CancelledAt = now;
        CancelledByAccountId = actorAccountId;
        Version++;
    }

    private static string? Normalize(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
