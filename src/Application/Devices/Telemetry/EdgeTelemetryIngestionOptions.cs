namespace Application.Devices.Telemetry;

public sealed class EdgeTelemetryIngestionOptions
{
    public const string SectionName = "EdgeTelemetryIngestion";

    public int MaxFutureClockSkewSeconds { get; set; } = 300;
    public int HeartbeatTimeoutSeconds { get; set; } = 90;
    public int ConnectivityReconciliationIntervalSeconds { get; set; } = 15;
    public int ConnectivityReconciliationBatchSize { get; set; } = 100;
    public int MaxBatchEventCount { get; set; } = 100;
    public int AlertCorrelationWindowMinutes { get; set; } = 15;
    public int AlertAutomationMaxEventAgeMinutes { get; set; } = 60;
    public int ReadinessTimeoutSeconds { get; set; } = 120;
}
