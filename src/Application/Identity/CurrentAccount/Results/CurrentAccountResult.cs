namespace Application.Identity.CurrentAccount.Results;

public sealed class CurrentAccountResult
{
    public Guid Id { get; set; }

    public string UserName { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public bool EmailConfirmed { get; set; }

    public string? FullName { get; set; }

    public string? ImageUrl { get; set; }

    public string? PhoneNumber { get; set; }

    public bool PhoneNumberConfirmed { get; set; }

    public string? Address { get; set; }

    public string Gender { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;

    public bool LocalLoginEnabled { get; set; }

    public bool GoogleLoginEnabled { get; set; }

    public string? GoogleEmail { get; set; }

    public DateTimeOffset? LastLoginAt { get; set; }

    public List<CurrentAccountRoleResult> Roles { get; set; } = new();
}

public sealed class CurrentAccountRoleResult
{
    public string RoleCode { get; set; } = string.Empty;

    public Guid? OrganizationId { get; set; }

    public Guid? StoreId { get; set; }

    public Guid? KioskId { get; set; }
}
