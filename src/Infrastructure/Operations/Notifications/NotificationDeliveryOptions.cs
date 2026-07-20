namespace Infrastructure.Operations.Notifications;

public sealed class NotificationDeliveryOptions
{
    public const string SectionName = "NotificationDelivery";
    public bool Enabled { get; set; } = true;
    public int IntervalSeconds { get; set; } = 10;
    public int BatchSize { get; set; } = 50;
    public int ProcessingTimeoutSeconds { get; set; } = 120;
    public int BaseRetryDelaySeconds { get; set; } = 30;
}
