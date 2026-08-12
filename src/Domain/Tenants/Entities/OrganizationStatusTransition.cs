using Domain.Common;
using Domain.Common.Enums;
using Domain.Tenants.Enums;

namespace Domain.Tenants.Entities;

public sealed class OrganizationStatusTransition : GuidEntity, IAuditable
{
    public Guid OrganizationId { get; set; }

    public EntityStatus FromStatus { get; set; }

    public EntityStatus ToStatus { get; set; }

    public string? ReasonCode { get; set; }

    public string Reason { get; set; } = null!;

    public Guid ChangedByAccountId { get; set; }

    public DateTimeOffset ChangedAt { get; set; }

    public string? RequestIdempotencyKey { get; set; }

    public long OrganizationStatusRevision { get; set; }

    public bool? ReadinessConfirmed { get; set; }

    public OrganizationLifecycleSideEffectStatus SessionRevocationStatus { get; set; }

    public int SessionRevocationAttemptCount { get; set; }

    public DateTimeOffset? NextSessionRevocationAttemptAt { get; set; }

    public DateTimeOffset? SessionRevocationCompletedAt { get; set; }

    public string? SessionRevocationLastError { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset? UpdatedAt { get; set; }

    public Guid? CreatedByAccountId { get; set; }

    public Guid? UpdatedByAccountId { get; set; }
}
