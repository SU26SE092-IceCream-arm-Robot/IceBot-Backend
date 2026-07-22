using System.Diagnostics;
using System.Diagnostics.Metrics;
using Application.Devices.Credentials.Commands;

namespace Infrastructure.Devices.Credentials.Observability;

public static class MqttCredentialReconciliationMetrics
{
    public const string MeterName = "IceBot.MqttCredentialLifecycle";

    private static readonly Meter Meter = new(MeterName, "1.0.0");
    private static readonly Counter<long> Outcomes =
        Meter.CreateCounter<long>("icebot.mqtt.credentials.reconciliation.outcomes");
    private static readonly Counter<long> Timeouts =
        Meter.CreateCounter<long>("icebot.mqtt.credentials.operation.timeouts");
    private static readonly Counter<long> RevocationRetries =
        Meter.CreateCounter<long>("icebot.mqtt.credentials.revocation.retry.attempts");
    private static long _staleCandidateCount;

    private static readonly ObservableGauge<long> StaleCandidates = Meter.CreateObservableGauge(
        "icebot.mqtt.credentials.stale.candidates",
        () => Interlocked.Read(ref _staleCandidateCount));

    public static void SetStaleCandidateCount(int count) =>
        Interlocked.Exchange(ref _staleCandidateCount, Math.Max(count, 0));

    public static void RecordOutcome(MqttCredentialReconciliationOutcome outcome)
    {
        Outcomes.Add(1, new TagList { { "outcome", outcome.ToString() } });
        if (outcome == MqttCredentialReconciliationOutcome.ProvisioningMarkedFailed)
        {
            Timeouts.Add(1, new TagList { { "operation", "provision" } });
        }
        else if (outcome == MqttCredentialReconciliationOutcome.RotationMarkedFailed)
        {
            Timeouts.Add(1, new TagList { { "operation", "rotation" } });
        }
        else if (outcome is MqttCredentialReconciliationOutcome.Revoked or
                 MqttCredentialReconciliationOutcome.RevokeRetryFailed)
        {
            RevocationRetries.Add(1, new TagList
            {
                { "outcome", outcome == MqttCredentialReconciliationOutcome.Revoked ? "succeeded" : "failed" }
            });
        }
    }
}
