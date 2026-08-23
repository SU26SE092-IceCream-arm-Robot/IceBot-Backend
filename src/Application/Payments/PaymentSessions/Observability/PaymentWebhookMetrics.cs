using System.Diagnostics.Metrics;

namespace Application.Payments.PaymentSessions.Observability;

public static class PaymentWebhookMetrics
{
    public const string MeterName = "IceBot.Payments.Webhooks";
    private static readonly Meter Meter = new(MeterName, "1.0.0");
    private static readonly Counter<long> VerifiedUnmatchedCallbacks = Meter.CreateCounter<long>(
        "icebot.payment.webhook.verified_unmatched",
        "{callback}",
        "Verified provider callbacks acknowledged without a matching local payment transaction.");
    private static readonly Counter<long> VerifiedEventConflicts = Meter.CreateCounter<long>(
        "icebot.payment.webhook.verified_event_conflict",
        "{callback}",
        "Verified provider callbacks whose event identity conflicts with a prior callback.");

    public static void RecordVerifiedUnmatched() => VerifiedUnmatchedCallbacks.Add(1);

    public static void RecordVerifiedEventConflict() => VerifiedEventConflicts.Add(1);
}
