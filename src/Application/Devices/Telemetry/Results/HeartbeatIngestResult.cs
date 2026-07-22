namespace Application.Devices.Telemetry.Results;

public sealed class HeartbeatIngestResult
{
    public Guid HeartbeatId { get; init; }
    public long HeartbeatSequence { get; init; }
    public DateTimeOffset ReceivedAt { get; init; }
    public bool Duplicate { get; init; }
    public bool Stale { get; init; }
}
