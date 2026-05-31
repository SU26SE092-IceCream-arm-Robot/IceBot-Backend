namespace Application.Identity.PasswordReset.Requests;

public sealed class RequestPasswordResetRequest
{
    public string EmailOrUserName { get; set; } = string.Empty;
}
