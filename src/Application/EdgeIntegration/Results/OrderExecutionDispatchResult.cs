namespace Application.EdgeIntegration.Results;

public sealed class OrderExecutionDispatchResult
{
    public Guid OrderId { get; init; }
    public Guid EdgeCommandId { get; init; }
    public Guid KioskExecutionEndpointId { get; init; }
    public Guid ConfigurationReleaseId { get; init; }
    public int DispatchAttemptNo { get; init; }
    public DateTimeOffset CommandExpiryAt { get; init; }
    public bool Existing { get; init; }
}
