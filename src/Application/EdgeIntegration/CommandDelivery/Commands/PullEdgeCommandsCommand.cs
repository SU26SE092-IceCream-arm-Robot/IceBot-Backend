namespace Application.EdgeIntegration.CommandDelivery.Commands;

public sealed class PullEdgeCommandsCommand
{
    public required Guid KioskId { get; init; }
    public required Guid EndpointId { get; init; }
    public required int MaxCommands { get; init; }
    public DateTimeOffset? EdgeTime { get; init; }
}
