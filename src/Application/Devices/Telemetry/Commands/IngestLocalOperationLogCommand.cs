using Domain.Common.Enums;

namespace Application.Devices.Telemetry.Commands;

public sealed class IngestLocalOperationLogCommand
{
    public required Guid KioskId { get; init; }
    public required Guid EndpointId { get; init; }
    public required Guid OriginNodeId { get; init; }
    public required Guid SourceEventId { get; init; }
    public Guid? DeviceId { get; init; }
    public Guid? OrderId { get; init; }
    public Guid? CorrelationId { get; init; }
    public Guid? CausationId { get; init; }
    public required string Action { get; init; }
    public string Category { get; init; } = "System";
    public SeverityLevel Severity { get; init; } = SeverityLevel.Info;
    public required string Message { get; init; }
    public required DateTimeOffset OccurredAt { get; init; }
    public string? PayloadJson { get; init; }
}
