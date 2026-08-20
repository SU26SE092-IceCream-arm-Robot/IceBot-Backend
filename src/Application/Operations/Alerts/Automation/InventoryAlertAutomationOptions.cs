namespace Application.Operations.Alerts.Automation;

public sealed class InventoryAlertAutomationOptions
{
    public const string SectionName = "InventoryAlertAutomation";
    public bool Enabled { get; set; } = true;
    public int IntervalSeconds { get; set; } = 30;
    public int BatchSize { get; set; } = 100;
    public int MaxBatchesPerRun { get; set; } = 20;
}
