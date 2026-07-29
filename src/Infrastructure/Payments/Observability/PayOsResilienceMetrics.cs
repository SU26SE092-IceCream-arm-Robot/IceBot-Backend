using System.Diagnostics.Metrics;

namespace Infrastructure.Payments.Observability;

public static class PayOsResilienceMetrics
{
    public const string MeterName = "IceBot.Payments.PayOS";

    private static readonly Meter Meter = new(MeterName, "1.0.0");
    private static readonly Counter<long> Failures = Meter.CreateCounter<long>(
        "icebot.payos.request.failures",
        "{failure}",
        "PayOS request failures classified by bounded failure kind.");

    public static void RecordTimeout(string operation = "create_payment_session") => Record(operation, "timeout");

    public static void RecordCircuitOpen(string operation = "create_payment_session") => Record(operation, "circuit_open");

    public static void RecordTransientFailure(string operation = "create_payment_session") => Record(operation, "transient");

    private static void Record(string operation, string failureKind)
    {
        Failures.Add(
            1,
            new KeyValuePair<string, object?>("provider", "PayOS"),
            new KeyValuePair<string, object?>("operation", operation),
            new KeyValuePair<string, object?>("failure.kind", failureKind));
    }
}
