using System.Text.Json;
using System.Text.Json.Serialization;
using Application.Devices.Connectivity.Contracts;
using Domain.Common.Enums;
using Domain.Devices.Catalog;
using Domain.Devices.ExecutionEndpoints;
using Domain.Devices.Telemetry;
using Domain.ProductionExecution.Enums;
using Domain.Inventory.Enums;

namespace Application.EdgeIntegration.Uplink;

public static class EdgeUplinkMessageTypes
{
    public const string Heartbeat = "heartbeat";
    public const string TelemetryEvents = "telemetry-events";
    public const string Readiness = "readiness";
    public const string ReportedDevices = "reported-devices";
    public const string ExecutionReport = "execution-report";
    public const string ProductionEvents = "production-events";
    public const string StateSummaries = "state-summaries";
    public const string InventoryObservations = "inventory-observations";

    public static readonly IReadOnlySet<string> All = new HashSet<string>(
        [
            Heartbeat,
            TelemetryEvents,
            Readiness,
            ReportedDevices,
            ExecutionReport,
            ProductionEvents,
            StateSummaries,
            InventoryObservations
        ],
        StringComparer.Ordinal);
}

public static class EdgeUplinkJson
{
    public static JsonSerializerOptions CreateSerializerOptions()
    {
        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = false,
            UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow
        };
        options.Converters.Add(new JsonStringEnumConverter(
            JsonNamingPolicy.CamelCase,
            allowIntegerValues: false));
        return options;
    }
}

public sealed class EdgeUplinkEnvelope
{
    public int SchemaVersion { get; init; } = 1;
    public Guid MessageId { get; init; }
    public DateTimeOffset SentAt { get; init; }
    public JsonElement Payload { get; init; }
}

public sealed class EdgeUplinkResult
{
    public int SchemaVersion { get; init; } = 1;
    public required Guid MessageId { get; init; }
    public required Guid EndpointId { get; init; }
    public required string MessageType { get; init; }
    public required DateTimeOffset ProcessedAt { get; init; }
    public required bool Succeeded { get; init; }
    public required int StatusCode { get; init; }
    public required bool Retryable { get; init; }
    public string? Message { get; init; }
    public object? Data { get; init; }
}

public sealed class EdgeHeartbeatUplink
{
    public Guid OriginNodeId { get; init; }
    public long HeartbeatSequence { get; init; }
    public DateTimeOffset ReportedAt { get; init; }
    public KioskHeartbeatStatus Status { get; init; } = KioskHeartbeatStatus.Online;
    public string? RobotStatus { get; init; }
    public string? NetworkStatus { get; init; }
    public string? AppVersion { get; init; }
    public string? FirmwareVersion { get; init; }
    public decimal? CpuUsagePercent { get; init; }
    public decimal? MemoryUsagePercent { get; init; }
    public decimal? DiskUsagePercent { get; init; }
    public int PendingSyncEventCount { get; init; }
}

public sealed class EdgeTelemetryBatchUplink
{
    public Guid OriginNodeId { get; init; }
    public IReadOnlyList<EdgeTelemetryEventUplink> Events { get; init; } = [];
}

public sealed class EdgeTelemetryEventUplink
{
    public Guid EventId { get; init; }
    public EdgeTelemetryEventType EventType { get; init; }
    public EdgeHeartbeatUplink? Heartbeat { get; init; }
    public EdgeDeviceEventUplink? DeviceEvent { get; init; }
    public EdgeLocalLogUplink? LocalLog { get; init; }
}

public enum EdgeTelemetryEventType
{
    Heartbeat = 1,
    DeviceEvent = 2,
    LocalLog = 3
}

public sealed class EdgeDeviceEventUplink
{
    public Guid DeviceId { get; init; }
    public Guid? CorrelationId { get; init; }
    public Guid? CausationId { get; init; }
    public string EventType { get; init; } = string.Empty;
    public SeverityLevel Severity { get; init; }
    public string Message { get; init; } = string.Empty;
    public DateTimeOffset OccurredAt { get; init; }
    public JsonElement? Payload { get; init; }
}

public sealed class EdgeLocalLogUplink
{
    public Guid? DeviceId { get; init; }
    public Guid? OrderId { get; init; }
    public Guid? CorrelationId { get; init; }
    public Guid? CausationId { get; init; }
    public string Action { get; init; } = string.Empty;
    public string Category { get; init; } = "System";
    public SeverityLevel Severity { get; init; } = SeverityLevel.Info;
    public string Message { get; init; } = string.Empty;
    public DateTimeOffset OccurredAt { get; init; }
    public JsonElement? Payload { get; init; }
}

public sealed class EdgeReadinessUplink
{
    public Guid SourceExecutorId { get; init; }
    public long StateRevision { get; init; }
    public DateTimeOffset ExecutorReportedAt { get; init; }
    public ExecutionReadinessState Readiness { get; init; }
    public ExecutionActivityState Activity { get; init; }
    public ExecutionSafetyState Safety { get; init; }
    public Guid? CurrentCommandId { get; init; }
    public PhysicalOutputState PhysicalOutputState { get; init; } = PhysicalOutputState.Unknown;
    public string? FaultCode { get; init; }
    public EdgeLocalPersistenceHealthUplink? LocalPersistenceHealth { get; init; }
    public IReadOnlyList<EdgeExecutionCapabilityUplink> Capabilities { get; init; } = [];
}

public sealed class EdgeLocalPersistenceHealthUplink
{
    public bool StorageWritable { get; init; }
    public long FreeSpaceBytes { get; init; }
    public long MinimumRequiredFreeSpaceBytes { get; init; }
    public LocalDatabaseHealth LocalDatabaseHealth { get; init; }
    public int PendingEventCount { get; init; }
    public int MaximumPendingEventCount { get; init; }
}

public sealed class EdgeExecutionCapabilityUplink
{
    public string CapabilityCode { get; init; } = string.Empty;
    public string? WorkcellCode { get; init; }
    public bool IsAvailable { get; init; }
    public string? UnavailableReason { get; init; }
}

public sealed class EdgeReportedDevicesUplink
{
    public Guid SourceExecutorId { get; init; }
    public long SnapshotRevision { get; init; }
    public DateTimeOffset ObservedAt { get; init; }
    public IReadOnlyList<EdgeReportedDeviceUplink> Devices { get; init; } = [];
}

public sealed class EdgeReportedDeviceUplink
{
    public string SourceDeviceKey { get; init; } = string.Empty;
    public Guid? DeviceId { get; init; }
    public string RuntimeTargetCode { get; init; } = string.Empty;
    public string MachineModelCode { get; init; } = string.Empty;
}


public sealed class EdgeExecutionReportUplink
{
    public Guid CommandId { get; init; }
    public Guid SourceEventId { get; init; }
    public long SequenceNumber { get; init; }
    public DateTimeOffset EdgeCreatedAt { get; init; }
    public DateTimeOffset? ExecutorReportedAt { get; init; }
    public string ReportType { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public Guid? DeploymentId { get; init; }
    public Guid? SourceProductionJobId { get; init; }
    public Guid? OrderItemId { get; init; }
    public int? ProductionUnitNo { get; init; }
    public int? ProductionUnitQuantity { get; init; }
    public Guid? WorkcellId { get; init; }
    public Guid? ControllerId { get; init; }
    public string? ExecutionPlanChecksum { get; init; }
    public long? ActiveSetVersion { get; init; }
    public string? ActiveSetChecksum { get; init; }
    public Guid? SourceConfigurationReleaseId { get; init; }
    public string? ReleaseChecksum { get; init; }
    public bool? PhysicalOutputMayHaveOccurred { get; init; }
    public string? ErrorCode { get; init; }
    public string? ErrorMessage { get; init; }
    public string? PayloadJson { get; init; }
    public IReadOnlyList<EdgeStockMovementEvidenceUplink> StockMovements { get; init; } = [];
}

public sealed class EdgeStockMovementEvidenceUplink
{
    public Guid SourceEventId { get; init; }
    public Guid OrderItemId { get; init; }
    public Guid IngredientDispenserStateId { get; init; }
    public decimal QuantityConsumed { get; init; }
    public decimal? BalanceAfter { get; init; }
    public DateTimeOffset? OccurredAt { get; init; }
    public bool IsEstimated { get; init; }
}

public sealed class EdgeProductionEventsUplink
{
    public Guid SourceExecutorId { get; init; }
    public IReadOnlyList<EdgeProductionEventUplink> Events { get; init; } = [];
}

public sealed class EdgeProductionEventUplink
{
    public Guid EventId { get; init; }
    public long SequenceNumber { get; init; }
    public string EventType { get; init; } = string.Empty;
    public int SchemaVersion { get; init; } = 1;
    public DateTimeOffset EdgeCreatedAt { get; init; }
    public Guid? OrderId { get; init; }
    public Guid? SourceCommandId { get; init; }
    public Guid? ProductionJobId { get; init; }
    public JsonElement? Payload { get; init; }
}

public sealed class EdgeStateSummariesUplink
{
    public Guid SourceExecutorId { get; init; }
    public IReadOnlyList<EdgeStateSummaryUplink> Summaries { get; init; } = [];
}

public sealed class EdgeStateSummaryUplink
{
    public string SummaryKind { get; init; } = string.Empty;
    public long StateRevision { get; init; }
    public int SummarySchemaVersion { get; init; } = 1;
    public DateTimeOffset EdgeCreatedAt { get; init; }
    public JsonElement Payload { get; init; }
}

public sealed class EdgeInventoryObservationsUplink
{
    public Guid SourceExecutorId { get; init; }
    public IReadOnlyList<EdgeInventorySensorObservationUplink> Observations { get; init; } = [];
}

public sealed class EdgeInventorySensorObservationUplink
{
    public Guid SourceEventId { get; init; }
    public Guid IngredientDispenserStateId { get; init; }
    public Guid DeviceId { get; init; }
    public long ObservationSequence { get; init; }
    public IngredientLevelStatus ObservedLevelStatus { get; init; }
    public DateTimeOffset ObservedAt { get; init; }
    public JsonElement? SensorPayload { get; init; }
}
