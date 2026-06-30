namespace Application.Devices.Results;

public sealed class DeviceEventIngestResult
{
    public Guid DeviceEventId { get; init; }
    public Guid EventId { get; init; }
    public DateTimeOffset ReceivedAt { get; init; }
    public bool Duplicate { get; init; }
}
