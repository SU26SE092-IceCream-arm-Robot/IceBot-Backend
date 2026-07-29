namespace Application.EdgeIntegration.Timeouts.Commands;

public sealed class ReconcileOrderExecutionTimeoutCommand
{
    public required Guid SourceCommandId { get; init; }
    public required DateTimeOffset ObservedAt { get; init; }
}
