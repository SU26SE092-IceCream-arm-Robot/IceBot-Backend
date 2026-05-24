namespace Application.Identity.CurrentAccount.Requests;

public sealed class UpdateCurrentAccountProfileRequest
{
    public string? FullName { get; set; }

    public string? PhoneNumber { get; set; }

    public string? Address { get; set; }

    public string? Gender { get; set; }

    public string? ImageUrl { get; set; }
}
