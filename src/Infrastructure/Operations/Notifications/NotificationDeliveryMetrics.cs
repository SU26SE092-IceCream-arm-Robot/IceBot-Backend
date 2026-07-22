using System.Diagnostics.Metrics;
using Domain.Operations.Enums;

namespace Infrastructure.Operations.Notifications;

public static class NotificationDeliveryMetrics
{
    public const string MeterName = "IceBot.Operations.Notifications";

    private static readonly Meter Meter = new(MeterName);
    private static readonly Counter<long> Outcomes = Meter.CreateCounter<long>(
        "icebot.notification_delivery.outcomes");
    private static readonly Histogram<double> ProcessingLag = Meter.CreateHistogram<double>(
        "icebot.notification_delivery.processing_lag", "s");
    private static readonly Histogram<int> DueBatchSize = Meter.CreateHistogram<int>(
        "icebot.notification_delivery.due_batch_size", "{delivery}");

    public static void RecordBatchSize(int count) => DueBatchSize.Record(count);

    public static void RecordClaim(DateTimeOffset dueAt, DateTimeOffset claimedAt) =>
        ProcessingLag.Record(Math.Max(0, (claimedAt - dueAt).TotalSeconds));

    public static void RecordOutcome(NotificationDeliveryStatus status, string notificationType) =>
        Outcomes.Add(1,
            new KeyValuePair<string, object?>("status", status.ToString()),
            new KeyValuePair<string, object?>("notification.type", notificationType));
}
