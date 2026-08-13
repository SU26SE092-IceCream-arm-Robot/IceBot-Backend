using Domain.Common;
using Domain.Identity.Enums;

namespace Domain.Identity.Entities;

public sealed class StaffWorkforceLifecycleTransition : BusinessEntity
{
    public Guid OrganizationId { get; set; }
    public Guid AccountId { get; set; }
    public AccountStatus FromStatus { get; set; }
    public AccountStatus ToStatus { get; set; }
    public string Reason { get; set; } = null!;
    public string ActorRoleCode { get; set; } = null!;
    public Guid? ActorOrganizationId { get; set; }
    public Guid? ActorStoreId { get; set; }
    public string? RequestIdempotencyKey { get; set; }
    public long WorkforceRevision { get; set; }
}
