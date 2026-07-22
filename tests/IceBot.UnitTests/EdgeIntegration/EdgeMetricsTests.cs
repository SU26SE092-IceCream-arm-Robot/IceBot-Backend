using System.Diagnostics.Metrics;
using System.Collections.Concurrent;
using Application.EdgeIntegration.Observability;

namespace IceBot.UnitTests.EdgeIntegration;

public sealed class EdgeMetricsTests
{
    [Fact]
    public void RecordsBoundedOperationalMetricTags()
    {
        using var listener = new MeterListener();
        var observed = new ConcurrentQueue<(string Name, double Value, Dictionary<string, object?> Tags)>();
        listener.InstrumentPublished = (instrument, meterListener) =>
        {
            if (instrument.Meter.Name == IceBotEdgeMetrics.MeterName)
            {
                meterListener.EnableMeasurementEvents(instrument);
            }
        };
        listener.SetMeasurementEventCallback<double>((instrument, value, tags, _) =>
        {
            if (instrument.Name == "icebot.edge.command.ack.latency")
                observed.Enqueue((instrument.Name, value,
                    tags.ToArray().ToDictionary(item => item.Key, item => item.Value)));
        });
        listener.Start();

        IceBotEdgeMetrics.RecordCommandAck(
            TimeSpan.FromSeconds(2),
            "ExecuteOrder",
            "Accepted");

        var metric = Assert.Single(observed);
        Assert.Equal("icebot.edge.command.ack.latency", metric.Name);
        Assert.Equal(2, metric.Value);
        Assert.Equal("ExecuteOrder", metric.Tags["command.type"]);
        Assert.Equal("Accepted", metric.Tags["ack.status"]);
        Assert.DoesNotContain("command.id", metric.Tags.Keys);
        Assert.DoesNotContain("endpoint.id", metric.Tags.Keys);
    }
}
