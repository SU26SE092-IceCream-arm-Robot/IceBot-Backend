namespace Application.EdgeIntegration.Commands;

public sealed class DispatchOrderExecutionCommand
{
    public required Guid OrderId { get; init; }
    public required int DispatchAttemptNo { get; init; }
    public DateTimeOffset? CommandExpiryAt { get; init; }
}
