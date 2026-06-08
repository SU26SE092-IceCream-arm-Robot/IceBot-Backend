namespace Application.Identity.Invitations.Requests;

public sealed class CreateAccountInvitationRequest
{
    public bool SendEmail { get; set; } = true;
}
