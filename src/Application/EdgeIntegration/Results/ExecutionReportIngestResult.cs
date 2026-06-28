namespace Application.EdgeIntegration.Results;

public sealed class ExecutionReportIngestResult
{
    public Guid CommandId { get; init; }
    public Guid SourceEventId { get; init; }
    public string ReportType { get; init; } = null!;
    public string Status { get; init; } = null!;
    public bool Applied { get; init; }
    public bool Duplicate { get; init; }
}
