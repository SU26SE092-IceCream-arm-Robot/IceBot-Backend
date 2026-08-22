using Domain.Common;

namespace Domain.Identity.Entities;

public sealed class TechnicianSupportGrantHistory : BusinessEntity
{
    public Guid AccountId { get; set; }
    public Guid? OrganizationId { get; set; }
    public Guid? StoreId { get; set; }
    public Guid? KioskId { get; set; }
    public string Action { get; set; } = null!;
    public string Reason { get; set; } = null!;
    public long AuthorizationVersion { get; set; }
    public Guid? ActorAccountId { get; set; }
}
