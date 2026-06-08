using System;

namespace Application.Identity.Invitations.Results;

public sealed class AccountInvitationResult
{
    public Guid AccountId { get; set; }

    public string InvitationToken { get; set; } = string.Empty;

    public string? InvitationUrl { get; set; }

    public DateTimeOffset ExpiresAt { get; set; }

    public DateTimeOffset? EmailSentAt { get; set; }

    public bool EmailSent { get; set; }
}
