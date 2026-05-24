namespace Application.Identity.PasswordReset.Requests;

public sealed class ResetPasswordRequest
{
    public string Token { get; set; } = string.Empty;

    public string NewPassword { get; set; } = string.Empty;
}
