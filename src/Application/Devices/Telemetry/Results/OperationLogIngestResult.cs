namespace Application.Devices.Telemetry.Results;

public sealed class OperationLogIngestResult
{
    public Guid OperationLogId { get; init; }
    public Guid SourceEventId { get; init; }
    public DateTimeOffset ReceivedAt { get; init; }
    public bool Duplicate { get; init; }
}
