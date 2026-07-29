using System.Diagnostics.Metrics;

namespace Application.EdgeIntegration.Observability;

public static class IceBotEdgeMetrics
{
    public const string MeterName = "IceBot.EdgeIntegration";
    private static readonly Meter Meter = new(MeterName, "1.0.0");

    private static readonly Counter<long> MqttWakeUpPublishAttempts = Meter.CreateCounter<long>(
        "icebot.mqtt.wakeup.publish.attempts", "{attempt}", "MQTT command wake-up publish attempts.");
    private static readonly Counter<long> MqttUplinkMessages = Meter.CreateCounter<long>(
        "icebot.mqtt.uplink.messages", "{message}", "MQTT Edge uplink processing outcomes.");
    private static readonly Histogram<double> MqttUplinkProcessingLatency = Meter.CreateHistogram<double>(
        "icebot.mqtt.uplink.processing.latency", "s", "MQTT Edge uplink application processing latency.");
    private static readonly Histogram<double> CommandPullLatency = Meter.CreateHistogram<double>(
        "icebot.edge.command.pull.latency", "s", "Time from durable command creation until command pull delivery.");
    private static readonly Histogram<double> CommandAckLatency = Meter.CreateHistogram<double>(
        "icebot.edge.command.ack.latency", "s", "Time from command delivery until first state-changing acknowledgement.");
    private static readonly Histogram<double> ExecutionReportLag = Meter.CreateHistogram<double>(
        "icebot.edge.execution.report.lag", "s", "Time from executor report timestamp until Cloud receipt.");
    private static readonly Counter<long> ObservationTransitions = Meter.CreateCounter<long>(
        "icebot.edge.execution.observation.transitions", "{transition}", "Cloud observation transitions for active executions.");
    private static readonly Histogram<double> StaleExecutionAge = Meter.CreateHistogram<double>(
        "icebot.edge.execution.stale.age", "s", "Age of the last executor report when execution becomes stale or unreachable.");

    private static long _staleExecutionCount;
    private static long _unreachableExecutionCount;

    static IceBotEdgeMetrics()
    {
        Meter.CreateObservableGauge(
            "icebot.edge.execution.observed",
            ObserveExecutionCounts,
            "{execution}",
            "Current active execution projections grouped by stale/unreachable observation.");
    }

    public static void RecordMqttWakeUp(string outcome, string commandType) =>
        MqttWakeUpPublishAttempts.Add(1,
            new KeyValuePair<string, object?>("outcome", outcome),
            new KeyValuePair<string, object?>("command.type", commandType));

    public static void RecordMqttUplink(string outcome, string messageType, TimeSpan processingTime)
    {
        MqttUplinkMessages.Add(1,
            new KeyValuePair<string, object?>("outcome", outcome),
            new KeyValuePair<string, object?>("message.type", messageType));
        MqttUplinkProcessingLatency.Record(
            NonNegativeSeconds(processingTime),
            new KeyValuePair<string, object?>("message.type", messageType));
    }

    public static void RecordCommandPull(TimeSpan latency, string commandType) =>
        CommandPullLatency.Record(NonNegativeSeconds(latency),
            new KeyValuePair<string, object?>("command.type", commandType));

    public static void RecordCommandAck(TimeSpan latency, string commandType, string acknowledgementStatus) =>
        CommandAckLatency.Record(NonNegativeSeconds(latency),
            new KeyValuePair<string, object?>("command.type", commandType),
            new KeyValuePair<string, object?>("ack.status", acknowledgementStatus));

    public static void RecordExecutionReportLag(TimeSpan lag, string reportType) =>
        ExecutionReportLag.Record(NonNegativeSeconds(lag),
            new KeyValuePair<string, object?>("report.type", reportType));

    public static void RecordObservationTransition(
        string observationStatus,
        string customerStatus,
        TimeSpan reportAge)
    {
        ObservationTransitions.Add(1,
            new KeyValuePair<string, object?>("observation.status", observationStatus),
            new KeyValuePair<string, object?>("customer.status", customerStatus));
        StaleExecutionAge.Record(NonNegativeSeconds(reportAge),
            new KeyValuePair<string, object?>("observation.status", observationStatus));
    }

    public static void SetObservedExecutionCounts(long stale, long unreachable)
    {
        Interlocked.Exchange(ref _staleExecutionCount, stale);
        Interlocked.Exchange(ref _unreachableExecutionCount, unreachable);
    }

    private static IEnumerable<Measurement<long>> ObserveExecutionCounts()
    {
        yield return new Measurement<long>(
            Interlocked.Read(ref _staleExecutionCount),
            new KeyValuePair<string, object?>("observation.status", "Stale"));
        yield return new Measurement<long>(
            Interlocked.Read(ref _unreachableExecutionCount),
            new KeyValuePair<string, object?>("observation.status", "Unreachable"));
    }

    private static double NonNegativeSeconds(TimeSpan value) => Math.Max(0, value.TotalSeconds);
}
