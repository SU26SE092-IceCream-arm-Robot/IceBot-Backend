namespace Infrastructure.Identity.Bootstrap;

public class IdentityBootstrapOptions
{
    public const string SectionName = "BootstrapAdmin";

    public string? UserName { get; set; }

    public string? Email { get; set; }

    public string? Password { get; set; }

    public string? FullName { get; set; }
}
