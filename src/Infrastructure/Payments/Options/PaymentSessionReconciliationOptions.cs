namespace Infrastructure.Payments.Options;

public sealed class PaymentSessionReconciliationOptions
{
    public const string SectionName = "Payments:SessionReconciliation";

    public bool Enabled { get; set; } = true;
    public int IntervalSeconds { get; set; } = 30;
    public int StaleAfterSeconds { get; set; } = 30;
    public int RetryDelaySeconds { get; set; } = 30;
    public int BatchSize { get; set; } = 50;
}
