using Domain.Common;

namespace Domain.Identity.Entities;

public sealed class TechnicianSupportScopeReplay : BusinessEntity
{
    public Guid AccountId { get; set; }
    public string IdempotencyKey { get; set; } = null!;
    public string RequestFingerprint { get; set; } = null!;
    public long AuthorizationVersion { get; set; }
}
