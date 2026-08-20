using System.Diagnostics.Metrics;
using Domain.Payments.Enums;

namespace Infrastructure.Payments.Observability;

public static class PaymentReconciliationMetrics
{
    public const string MeterName = "IceBot.Payments.Reconciliation";

    private static readonly Meter Meter = new(MeterName, "1.0.0");
    private static readonly Counter<long> Candidates = Meter.CreateCounter<long>(
        "icebot.payment_reconciliation.observation.candidates", unit: "payment");
    private static readonly Counter<long> Outcomes = Meter.CreateCounter<long>(
        "icebot.payment_reconciliation.observation.outcomes", unit: "observation");
    private static readonly Histogram<double> Duration = Meter.CreateHistogram<double>(
        "icebot.payment_reconciliation.observation.duration", unit: "s");

    public static void RecordCandidateCount(int count) => Candidates.Add(Math.Max(count, 0));

    public static void RecordOutcome(PaymentProviderObservationOutcome outcome, string provider) =>
        Outcomes.Add(1,
            new KeyValuePair<string, object?>("outcome", outcome.ToString()),
            new KeyValuePair<string, object?>("provider", provider));

    public static void RecordDuration(TimeSpan duration) => Duration.Record(Math.Max(0, duration.TotalSeconds));
}
