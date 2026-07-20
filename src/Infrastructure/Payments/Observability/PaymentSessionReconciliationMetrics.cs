using System.Diagnostics.Metrics;
using Application.Payments.PaymentSessions.Commands;

namespace Infrastructure.Payments.Observability;

public static class PaymentSessionReconciliationMetrics
{
    private static readonly Meter Meter = new(PayOsResilienceMetrics.MeterName, "1.0.0");
    private static readonly Counter<long> Outcomes = Meter.CreateCounter<long>(
        "icebot.payment_session.reconciliation.outcomes",
        "{outcome}",
        "Payment-session reconciliation outcomes.");
    private static readonly Counter<long> Interventions = Meter.CreateCounter<long>(
        "icebot.payment_session.interventions",
        "{intervention}",
        "Payment sessions requiring operator investigation.");
    private static readonly Histogram<double> PendingAge = Meter.CreateHistogram<double>(
        "icebot.payment_session.reconciliation.pending_age",
        "s",
        "Age of an incomplete payment session when reconciliation starts.");

    public static void Record(PaymentSessionReconciliationOutcome outcome)
    {
        Outcomes.Add(1, new KeyValuePair<string, object?>("outcome", outcome.ToString()));

        if (outcome is PaymentSessionReconciliationOutcome.AwaitingWebhook or
            PaymentSessionReconciliationOutcome.RetryExhausted or
            PaymentSessionReconciliationOutcome.IdentityMismatch or
            PaymentSessionReconciliationOutcome.AmountMismatch)
        {
            Interventions.Add(1, new KeyValuePair<string, object?>("intervention", outcome.ToString()));
        }
    }

    public static void RecordPendingAge(TimeSpan age) =>
        PendingAge.Record(Math.Max(age.TotalSeconds, 0));
}
