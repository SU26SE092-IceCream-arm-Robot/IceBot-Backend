namespace Application.Identity.Invitations.Results;

public sealed class AcceptInvitationResult
{
    public bool LocalLoginEnabled { get; set; }

    public bool GoogleLoginEnabled { get; set; }
}
