using Domain.Common;
using Domain.Common.Enums;
using Domain.Tenants.Enums;

namespace Domain.Tenants.Entities;

public partial class Organization : BusinessEntity
{
    public string Code { get; set; } = null!;

    public string Name { get; set; } = null!;

    public string? LegalName { get; set; }

    public string? TaxCode { get; set; }

    public string? Email { get; set; }

    public string? PhoneNumber { get; set; }

    public string? Address { get; set; }

    public EntityStatus Status { get; set; } = EntityStatus.Active;

    public long StatusRevision { get; private set; }

    public string? SuspensionReasonCode { get; private set; }

    public string? SuspensionReason { get; private set; }

    public DateTimeOffset? SuspendedAt { get; private set; }

    public Guid? SuspendedByAccountId { get; private set; }

    public string? DeactivationReason { get; private set; }

    public DateTimeOffset? DeactivatedAt { get; private set; }

    public Guid? DeactivatedByAccountId { get; private set; }

    public DateTimeOffset? ReactivatedAt { get; private set; }

    public Guid? ReactivatedByAccountId { get; private set; }

    public string? MetadataJson { get; set; }

    public virtual ICollection<Store> Stores { get; set; } = new List<Store>();

    public OrganizationStatusTransition Suspend(
        Guid actorId,
        string reasonCode,
        string reason,
        long expectedRevision,
        string? idempotencyKey,
        DateTimeOffset now) =>
        TransitionTo(EntityStatus.Suspended, actorId, reasonCode, reason, expectedRevision, idempotencyKey, now, readinessConfirmed: null);

    public OrganizationStatusTransition Resume(
        Guid actorId,
        string reason,
        long expectedRevision,
        string? idempotencyKey,
        DateTimeOffset now) =>
        TransitionTo(EntityStatus.Active, actorId, null, reason, expectedRevision, idempotencyKey, now, readinessConfirmed: null);

    public OrganizationStatusTransition Deactivate(
        Guid actorId,
        string reason,
        long expectedRevision,
        string? idempotencyKey,
        DateTimeOffset now) =>
        TransitionTo(EntityStatus.Inactive, actorId, "ServiceEnded", reason, expectedRevision, idempotencyKey, now, readinessConfirmed: null);

    public OrganizationStatusTransition Reactivate(
        Guid actorId,
        string reason,
        long expectedRevision,
        string? idempotencyKey,
        DateTimeOffset now,
        bool readinessConfirmed) =>
        TransitionTo(EntityStatus.Active, actorId, null, reason, expectedRevision, idempotencyKey, now, readinessConfirmed);

    private OrganizationStatusTransition TransitionTo(
        EntityStatus targetStatus,
        Guid actorId,
        string? reasonCode,
        string reason,
        long expectedRevision,
        string? idempotencyKey,
        DateTimeOffset now,
        bool? readinessConfirmed)
    {
        if (actorId == Guid.Empty)
        {
            throw new DomainRuleException("Organization lifecycle actor is required.");
        }

        if (StatusRevision != expectedRevision)
        {
            throw new DomainRuleException("Organization lifecycle state is stale.");
        }

        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new DomainRuleException("Organization lifecycle reason is required.");
        }

        if (!IsAllowedTransition(Status, targetStatus, readinessConfirmed))
        {
            throw new DomainRuleException($"Organization cannot transition from {Status} to {targetStatus}.");
        }

        var transition = new OrganizationStatusTransition
        {
            Id = Guid.NewGuid(),
            OrganizationId = Id,
            FromStatus = Status,
            ToStatus = targetStatus,
            ReasonCode = reasonCode,
            Reason = reason.Trim(),
            ChangedByAccountId = actorId,
            ChangedAt = now,
            RequestIdempotencyKey = string.IsNullOrWhiteSpace(idempotencyKey) ? null : idempotencyKey.Trim(),
            OrganizationStatusRevision = StatusRevision + 1,
            ReadinessConfirmed = readinessConfirmed,
            SessionRevocationStatus = targetStatus is EntityStatus.Suspended or EntityStatus.Inactive
                ? OrganizationLifecycleSideEffectStatus.Pending
                : OrganizationLifecycleSideEffectStatus.Completed,
            NextSessionRevocationAttemptAt = targetStatus is EntityStatus.Suspended or EntityStatus.Inactive
                ? now
                : null,
            CreatedAt = now,
            CreatedByAccountId = actorId
        };

        Status = targetStatus;
        StatusRevision = transition.OrganizationStatusRevision;
        UpdatedAt = now;
        UpdatedByAccountId = actorId;

        switch (targetStatus)
        {
            case EntityStatus.Suspended:
                SuspensionReasonCode = reasonCode;
                SuspensionReason = transition.Reason;
                SuspendedAt = now;
                SuspendedByAccountId = actorId;
                break;
            case EntityStatus.Inactive:
                DeactivationReason = transition.Reason;
                DeactivatedAt = now;
                DeactivatedByAccountId = actorId;
                break;
            case EntityStatus.Active when transition.FromStatus == EntityStatus.Inactive:
                ReactivatedAt = now;
                ReactivatedByAccountId = actorId;
                break;
        }

        return transition;
    }

    private static bool IsAllowedTransition(
        EntityStatus currentStatus,
        EntityStatus targetStatus,
        bool? readinessConfirmed) =>
        (currentStatus, targetStatus) switch
        {
            (EntityStatus.Active, EntityStatus.Suspended) => true,
            (EntityStatus.Suspended, EntityStatus.Active) => true,
            (EntityStatus.Active, EntityStatus.Inactive) => true,
            (EntityStatus.Suspended, EntityStatus.Inactive) => true,
            (EntityStatus.Inactive, EntityStatus.Active) => readinessConfirmed is true,
            _ => false
        };
}
