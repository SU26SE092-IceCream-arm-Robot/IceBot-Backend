namespace Infrastructure.Identity.Jobs;

public sealed class StaffSessionRevocationReconciliationOptions
{
    public const string SectionName = "StaffSessionRevocationReconciliation";

    public int IntervalSeconds { get; set; } = 30;

    public int BatchSize { get; set; } = 50;
}
