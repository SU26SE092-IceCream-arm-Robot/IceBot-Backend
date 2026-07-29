using System.Collections.Concurrent;
using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace Infrastructure.Operations.Automation;

public static class OperationalAutomationMetrics
{
    public const string MeterName = "IceBot.OperationalAutomation";

    private static readonly Meter Meter = new(MeterName, "1.0.0");
    private static readonly Counter<long> Runs = Meter.CreateCounter<long>("icebot.automation.runs");
    private static readonly Counter<long> CandidateFailures =
        Meter.CreateCounter<long>("icebot.automation.candidate.failures");
    private static readonly Histogram<double> RunDuration =
        Meter.CreateHistogram<double>("icebot.automation.run.duration", unit: "s");
    private static readonly ConcurrentDictionary<string, long> LastSuccessfulRunUnixSeconds = new();

    private static readonly ObservableGauge<double> LastSuccessfulRun = Meter.CreateObservableGauge(
        "icebot.automation.last_success.unix_time",
        () => LastSuccessfulRunUnixSeconds.Select(pair =>
            new Measurement<double>(pair.Value, new KeyValuePair<string, object?>("automation.job", pair.Key))),
        unit: "s");

    public static void RecordRun(string job, string outcome, TimeSpan duration)
    {
        var tags = new TagList { { "automation.job", job }, { "outcome", outcome } };
        Runs.Add(1, tags);
        RunDuration.Record(duration.TotalSeconds, tags);
        if (string.Equals(outcome, "succeeded", StringComparison.Ordinal))
        {
            LastSuccessfulRunUnixSeconds[job] = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        }
    }

    public static void RecordCandidateFailure(string job) =>
        CandidateFailures.Add(1, new TagList { { "automation.job", job } });
}
