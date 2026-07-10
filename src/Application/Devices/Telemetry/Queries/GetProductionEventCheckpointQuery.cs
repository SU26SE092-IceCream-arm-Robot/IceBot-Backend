namespace Application.Devices.Telemetry.Queries;

public sealed class GetProductionEventCheckpointQuery
{
    public required Guid KioskId { get; init; }
    public required Guid EndpointId { get; init; }
    public required Guid SourceExecutorId { get; init; }
}
