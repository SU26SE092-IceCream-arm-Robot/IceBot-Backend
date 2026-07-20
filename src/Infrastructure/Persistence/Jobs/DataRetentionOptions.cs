namespace Infrastructure.Persistence.Jobs;

public sealed class DataRetentionOptions
{
    public const string SectionName = "DataRetention";

    public bool Enabled { get; set; } = true;
    public int IntervalHours { get; set; } = 24;
    public int HeartbeatDays { get; set; } = 30;
    public int DeviceEventDays { get; set; } = 90;
    public int OperationLogDays { get; set; } = 90;
    public int ProcessedSyncInboxDays { get; set; } = 180;
    public int ExpiredIdentityCredentialDays { get; set; } = 30;
    public int NotificationDeliveryDays { get; set; } = 90;
    public int BatchSize { get; set; } = 1000;
    public int MaxBatchesPerRun { get; set; } = 20;
}
