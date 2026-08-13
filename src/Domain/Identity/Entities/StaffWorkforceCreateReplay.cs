using Domain.Common;

namespace Domain.Identity.Entities;

public sealed class StaffWorkforceCreateReplay : BusinessEntity
{
    public Guid OrganizationId { get; set; }
    public string IdempotencyKey { get; set; } = null!;
    public string RequestFingerprint { get; set; } = null!;
    public Guid AccountId { get; set; }
}
