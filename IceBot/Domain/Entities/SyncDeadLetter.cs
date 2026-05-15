using Domain.Common;
using Domain.Enums;
using Domain.Identity.Entities;

namespace Domain.Entities;

public partial class SyncDeadLetter : GuidEntity
{
    public Guid? SyncEventInboxId { get; set; }

    public Guid? EventId { get; set; }

    public Guid? KioskId { get; set; }

    public Guid? SourceNodeId { get; set; }

    public Guid? CorrelationId { get; set; }

    public Guid? CausationId { get; set; }

    public Guid? ResolvedByAccountId { get; set; }

    public string EventType { get; set; } = null!;

    public string? AggregateType { get; set; }

    public Guid? AggregateId { get; set; }

    public string PayloadJson { get; set; } = null!;

    public string ErrorMessage { get; set; } = null!;

    public string? ErrorDetails { get; set; }

    public SyncDeadLetterStatus Status { get; set; } = SyncDeadLetterStatus.Open;

    public int ProcessingAttempts { get; set; }

    public DateTimeOffset FailedAt { get; set; }

    public DateTimeOffset? ResolvedAt { get; set; }

    public string? ResolutionNotes { get; set; }

    public virtual Kiosk? Kiosk { get; set; }

    public virtual Account? ResolvedByAccount { get; set; }

    public virtual SyncEventInbox? SyncEventInbox { get; set; }

    public void Resolve(Guid resolvedByAccountId, DateTimeOffset resolvedAt, string? resolutionNotes = null)
    {
        if (Status == SyncDeadLetterStatus.Resolved)
        {
            throw new DomainRuleException("Sync dead letter is already resolved.");
        }

        ResolvedByAccountId = resolvedByAccountId;
        ResolvedAt = resolvedAt;
        ResolutionNotes = resolutionNotes;
        Status = SyncDeadLetterStatus.Resolved;
    }

    public void Ignore(Guid resolvedByAccountId, DateTimeOffset resolvedAt, string reason)
    {
        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new DomainRuleException("Ignore reason is required.");
        }

        ResolvedByAccountId = resolvedByAccountId;
        ResolvedAt = resolvedAt;
        ResolutionNotes = reason.Trim();
        Status = SyncDeadLetterStatus.Ignored;
    }
}
