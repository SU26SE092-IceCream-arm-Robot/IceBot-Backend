using Domain.Common;

namespace Domain.Identity.Entities;

public partial class AccountInvitation : GuidEntity
{
    public Guid AccountId { get; set; }

    public string TokenHash { get; set; } = null!;

    public DateTimeOffset InvitedAt { get; set; }

    public DateTimeOffset ExpiresAt { get; set; }

    public DateTimeOffset? EmailSentAt { get; set; }

    public DateTimeOffset? AcceptedAt { get; set; }

    public DateTimeOffset? RevokedAt { get; set; }

    public Guid? InvitedByAccountId { get; set; }

    public string? AcceptedByIp { get; set; }

    public string? AcceptedByUserAgent { get; set; }

    public string Purpose { get; set; } = "AccountInvitation";

    public virtual Account Account { get; set; } = null!;
}
