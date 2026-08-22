namespace Application.Abstractions.Realtime.Events;

public sealed record ExecutionReadinessChangedEvent
{
    public string Type => "ExecutionReadinessChanged";
    public required Guid KioskId { get; init; }
    public required Guid EndpointId { get; init; }
    public required long StateRevision { get; init; }
    public required string Readiness { get; init; }
    public required string Activity { get; init; }
    public required string Safety { get; init; }
    public required DateTimeOffset OccurredAt { get; init; }
}
