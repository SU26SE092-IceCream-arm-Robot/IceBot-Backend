using System.ComponentModel.DataAnnotations;

namespace Application.Tenants.Organizations.Requests;

public sealed class OrganizationLifecycleTransitionRequest
{
    [StringLength(100)]
    public string? ReasonCode { get; init; }

    [Required]
    [StringLength(1000)]
    public string? Reason { get; init; }

    [Range(0, long.MaxValue)]
    public long ExpectedRevision { get; init; }

    [StringLength(200)]
    public string? IdempotencyKey { get; init; }

    public bool ReadinessConfirmed { get; init; }
}
