namespace Application.Tenants.Organizations.Results;

public sealed class OrganizationStatusTransitionResult
{
    public Guid Id { get; init; }

    public string FromStatus { get; init; } = null!;

    public string ToStatus { get; init; } = null!;

    public string? ReasonCode { get; init; }

    public string Reason { get; init; } = null!;

    public Guid ChangedByAccountId { get; init; }

    public DateTimeOffset ChangedAt { get; init; }

    public long OrganizationStatusRevision { get; init; }

    public bool? ReadinessConfirmed { get; init; }
}
