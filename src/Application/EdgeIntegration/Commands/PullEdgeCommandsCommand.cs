namespace Application.EdgeIntegration.Commands;

public sealed class PullEdgeCommandsCommand
{
    public required Guid KioskId { get; init; }
    public required Guid EndpointId { get; init; }
    public required string Credential { get; init; }
    public required int MaxCommands { get; init; }
    public DateTimeOffset? EdgeTime { get; init; }
}
