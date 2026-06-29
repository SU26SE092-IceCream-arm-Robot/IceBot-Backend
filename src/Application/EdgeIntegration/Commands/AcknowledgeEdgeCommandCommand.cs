namespace Application.EdgeIntegration.Commands;

public sealed class AcknowledgeEdgeCommandCommand
{
    public required Guid KioskId { get; init; }
    public required Guid EndpointId { get; init; }
    public required Guid CommandId { get; init; }
    public required string AckStatus { get; init; }
    public DateTimeOffset? AcknowledgedAt { get; init; }
    public string? RejectionCode { get; init; }
    public string? RejectionMessage { get; init; }
    public bool? PhysicalOutputMayHaveOccurred { get; init; }
}
