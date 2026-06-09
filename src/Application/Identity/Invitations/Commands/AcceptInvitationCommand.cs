using Application.Identity.Invitations.Requests;

namespace Application.Identity.Invitations.Commands;

public sealed class AcceptInvitationCommand
{
    public AcceptInvitationRequest Request { get; init; } = null!;
    public string? IpAddress { get; init; }
    public string? UserAgent { get; init; }
}
