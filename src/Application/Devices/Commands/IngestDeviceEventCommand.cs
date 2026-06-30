using Domain.Common.Enums;

namespace Application.Devices.Commands;

public sealed class IngestDeviceEventCommand
{
    public required Guid KioskId { get; init; }
    public required Guid EndpointId { get; init; }
    public required Guid OriginNodeId { get; init; }
    public required Guid DeviceId { get; init; }
    public required Guid EventId { get; init; }
    public Guid? CorrelationId { get; init; }
    public Guid? CausationId { get; init; }
    public required string EventType { get; init; }
    public required SeverityLevel Severity { get; init; }
    public required string Message { get; init; }
    public required DateTimeOffset OccurredAt { get; init; }
    public string? PayloadJson { get; init; }
}
