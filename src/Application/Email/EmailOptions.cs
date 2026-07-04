namespace Application.Email;

public sealed class EmailOptions
{
    public const string SectionName = "Email";

    public string Host { get; set; } = string.Empty;

    public int Port { get; set; } = 587;

    public string From { get; set; } = string.Empty;

    public string? DisplayName { get; set; }

    public string? UserName { get; set; }

    public string? Password { get; set; }

    public bool EnableSsl { get; set; } = true;

    public int OperationTimeoutSeconds { get; set; } = 30;

    public string? PasswordResetBaseUrl { get; set; }

    public string? InvitationBaseUrl { get; set; }
}
