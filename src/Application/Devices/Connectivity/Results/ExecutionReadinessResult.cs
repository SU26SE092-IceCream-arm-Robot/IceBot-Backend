namespace Application.Devices.Connectivity.Results;

public sealed class ExecutionReadinessResult
{
    public Guid EndpointId { get; init; }
    public long StateRevision { get; init; }
    public bool Applied { get; init; }
    public bool DuplicateOrStale { get; init; }
    public required string Readiness { get; init; }
    public required string Activity { get; init; }
    public required string Safety { get; init; }
    public DateTimeOffset CloudReceivedAt { get; init; }
}
