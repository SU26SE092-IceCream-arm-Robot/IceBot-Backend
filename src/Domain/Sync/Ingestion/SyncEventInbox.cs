using Domain.Common;
using Domain.Sync.Enums;
using Domain.Tenants.Entities;

namespace Domain.Sync.Ingestion;

public partial class SyncEventInbox : GuidEntity
{
    public Guid EventId { get; set; }

    public Guid? KioskId { get; set; }

    public Guid SourceNodeId { get; set; }

    public long? SequenceNumber { get; set; }

    public Guid? CorrelationId { get; set; }

    public Guid? CausationId { get; set; }

    public string EventType { get; set; } = null!;

    public string? AggregateType { get; set; }

    public Guid? AggregateId { get; set; }

    public string PayloadJson { get; set; } = null!;

    public string? HeadersJson { get; set; }

    public SyncEventStatus Status { get; set; } = SyncEventStatus.Received;

    public DateTimeOffset OccurredAt { get; set; }

    public DateTimeOffset ReceivedAt { get; set; }

    public DateTimeOffset? ProcessedAt { get; set; }

    public DateTimeOffset? LastAttemptAt { get; set; }

    public DateTimeOffset? NextRetryAt { get; set; }

    public Guid? LockId { get; set; }

    public DateTimeOffset? LockedUntil { get; set; }

    public int ProcessingAttempts { get; set; }

    public int MaxProcessingAttempts { get; set; } = 5;

    public string? LastError { get; set; }

    public virtual Kiosk? Kiosk { get; set; }

    public void MarkProcessing(Guid lockId, DateTimeOffset attemptedAt, DateTimeOffset lockedUntil)
    {
        if (Status == SyncEventStatus.Processed)
        {
            throw new DomainRuleException("Cannot process an already processed sync event.");
        }

        if (LockedUntil.HasValue && LockedUntil.Value > attemptedAt && LockId != lockId)
        {
            throw new DomainRuleException("Sync event is locked by another processor.");
        }

        LockId = lockId;
        LockedUntil = lockedUntil;
        LastAttemptAt = attemptedAt;
        ProcessingAttempts++;
        Status = SyncEventStatus.Processing;
    }

    public void MarkProcessed(DateTimeOffset processedAt)
    {
        ProcessedAt = processedAt;
        LockId = null;
        LockedUntil = null;
        NextRetryAt = null;
        LastError = null;
        Status = SyncEventStatus.Processed;
    }

    public void MarkFailed(string error, DateTimeOffset? nextRetryAt)
    {
        LastError = error;
        NextRetryAt = nextRetryAt;
        LockId = null;
        LockedUntil = null;
        Status = CanRetry ? SyncEventStatus.Failed : SyncEventStatus.DeadLettered;
    }

    public void MarkIgnored(DateTimeOffset processedAt, string? reason = null)
    {
        ProcessedAt = processedAt;
        LastError = reason;
        LockId = null;
        LockedUntil = null;
        Status = SyncEventStatus.Ignored;
    }

    public bool CanRetry => ProcessingAttempts < MaxProcessingAttempts;

    public void PrepareManualRetry(DateTimeOffset requestedAt)
    {
        if (Status != SyncEventStatus.DeadLettered && Status != SyncEventStatus.Failed)
            throw new DomainRuleException("Only failed or dead-lettered sync events can be retried.");
        Status = SyncEventStatus.Failed;
        ProcessingAttempts = 0;
        NextRetryAt = requestedAt;
        LockId = null;
        LockedUntil = null;
    }
}
