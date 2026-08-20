namespace Application.Payments.Reconciliation;

public sealed class PaymentReconciliationOptions
{
    public const string SectionName = "Payments:DailyReconciliation";
    public string TimeZoneId { get; set; } = "Asia/Ho_Chi_Minh";
    public int EvidenceFreshnessMinutes { get; set; } = 60;
    public int ObservationIntervalSeconds { get; set; } = 900;
    public int ObservationLookbackDays { get; set; } = 2;
    public int ObservationBatchSize { get; set; } = 100;
}
