using System.ComponentModel.DataAnnotations;

namespace Application.Identity.Invitations.Requests;

public sealed class AcceptInvitationRequest
{
    [Required]
    public string Token { get; set; } = null!;

    public string? NewPassword { get; set; }
}
