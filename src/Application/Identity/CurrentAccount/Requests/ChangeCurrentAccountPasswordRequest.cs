namespace Application.Identity.CurrentAccount.Requests;

public sealed class ChangeCurrentAccountPasswordRequest
{
    public string CurrentPassword { get; set; } = string.Empty;

    public string NewPassword { get; set; } = string.Empty;
}
