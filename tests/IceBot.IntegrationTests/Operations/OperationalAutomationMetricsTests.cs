using System.Collections.Concurrent;
using System.Diagnostics.Metrics;
using Infrastructure.Operations.Automation;

namespace IceBot.IntegrationTests.Operations;

public sealed class OperationalAutomationMetricsTests
{
    [Fact]
    public void RecordsOnlyBoundedAutomationTags()
    {
        using var listener = new MeterListener();
        var observed = new ConcurrentQueue<(string Name, Dictionary<string, object?> Tags)>();
        listener.InstrumentPublished = (instrument, meterListener) =>
        {
            if (instrument.Meter.Name == OperationalAutomationMetrics.MeterName)
            {
                meterListener.EnableMeasurementEvents(instrument);
            }
        };
        listener.SetMeasurementEventCallback<long>((instrument, _, tags, _) =>
        {
            observed.Enqueue((
                instrument.Name,
                tags.ToArray().ToDictionary(item => item.Key, item => item.Value)));
        });
        listener.Start();

        OperationalAutomationMetrics.RecordRun(
            "order_execution_dispatch_reconciliation",
            "succeeded",
            TimeSpan.FromSeconds(2));
        OperationalAutomationMetrics.RecordCandidateFailure("order_execution_dispatch_reconciliation");

        var measurements = observed.ToArray();
        Assert.Contains(measurements, measurement =>
            measurement.Name == "icebot.automation.runs" &&
            (string?)measurement.Tags["automation.job"] == "order_execution_dispatch_reconciliation" &&
            (string?)measurement.Tags["outcome"] == "succeeded");
        Assert.Contains(measurements, measurement =>
            measurement.Name == "icebot.automation.candidate.failures" &&
            (string?)measurement.Tags["automation.job"] == "order_execution_dispatch_reconciliation");

        Assert.All(measurements, measurement =>
            Assert.DoesNotContain(measurement.Tags.Keys, key =>
                key.Contains("id", StringComparison.OrdinalIgnoreCase)));
    }
}
