namespace Application.Identity.Invitations.Results;

public sealed class AcceptInvitationResult
{
    public bool Accepted { get; set; }

    public bool LocalLoginEnabled { get; set; }

    public bool GoogleLoginEnabled { get; set; }
}
