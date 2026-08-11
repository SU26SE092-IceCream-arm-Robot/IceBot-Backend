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

    public static void RecordVerifiedUnmatched() => VerifiedUnmatchedCallbacks.Add(1);
}
