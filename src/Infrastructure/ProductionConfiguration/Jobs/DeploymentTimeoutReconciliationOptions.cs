namespace Infrastructure.ProductionConfiguration.Jobs;

public sealed class DeploymentTimeoutReconciliationOptions
{
    public const string SectionName = "DeploymentTimeoutReconciliation";

    public bool Enabled { get; set; } = true;
    public int IntervalSeconds { get; set; } = 60;
    public int MaxCommandsPerRun { get; set; } = 100;
    public int AcceptedReportTimeoutMinutes { get; set; } = 30;
    public int InstalledActivationTimeoutMinutes { get; set; } = 30;
}
