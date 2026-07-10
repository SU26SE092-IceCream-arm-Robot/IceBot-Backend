namespace Application.Devices.Telemetry.Commands;

public sealed class IngestEdgeStateSummariesCommand
{
    public required Guid KioskId { get; init; }
    public required Guid EndpointId { get; init; }
    public required Guid SourceExecutorId { get; init; }
    public required IReadOnlyList<EdgeStateSummaryItem> Summaries { get; init; }
}

public sealed class EdgeStateSummaryItem
{
    public required string SummaryKind { get; init; }
    public required long StateRevision { get; init; }
    public required int SummarySchemaVersion { get; init; }
    public required DateTimeOffset EdgeCreatedAt { get; init; }
    public required string PayloadJson { get; init; }
}
