using Domain.Common;

namespace Domain.Identity.Entities;

public partial class RefreshToken : GuidEntity
{
    public Guid AccountId { get; set; }

    public Guid? AccountDeviceId { get; set; }

    public Guid? ReplacedByTokenId { get; set; }

    public string TokenHash { get; set; } = null!;

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset ExpiresAt { get; set; }

    public DateTimeOffset? RevokedAt { get; set; }

    public string? CreatedByIp { get; set; }

    public string? RevokedByIp { get; set; }

    public string? CreatedByUserAgent { get; set; }

    public string? RevokedByUserAgent { get; set; }

    public string? RevokeReason { get; set; }

    public DateTimeOffset? ReuseDetectedAt { get; set; }

    public bool IsUsed { get; set; }

    public virtual Account Account { get; set; } = null!;

    public virtual AccountDevice? AccountDevice { get; set; }

    public virtual RefreshToken? ReplacedByToken { get; set; }
}
