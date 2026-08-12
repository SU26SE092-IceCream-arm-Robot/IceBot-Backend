namespace Infrastructure.Tenants.Jobs;

public sealed class OrganizationSessionRevocationOptions
{
    public const string SectionName = "OrganizationSessionRevocation";

    public int IntervalSeconds { get; set; } = 30;

    public int BatchSize { get; set; } = 25;

    public int RetryDelaySeconds { get; set; } = 60;
}
