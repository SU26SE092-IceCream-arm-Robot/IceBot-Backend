using Domain.Common;

namespace Domain.Identity.Entities;

public partial class PasswordResetRequest : GuidEntity
{
    public Guid AccountId { get; set; }

    public string TokenHash { get; set; } = null!;

    public DateTimeOffset RequestedAt { get; set; }

    public DateTimeOffset ExpiresAt { get; set; }

    public DateTimeOffset? UsedAt { get; set; }

    public string? RequestedByIp { get; set; }

    public string? RequestedByUserAgent { get; set; }

    public string? UsedByIp { get; set; }

    public string? UsedByUserAgent { get; set; }

    public virtual Account Account { get; set; } = null!;
}
