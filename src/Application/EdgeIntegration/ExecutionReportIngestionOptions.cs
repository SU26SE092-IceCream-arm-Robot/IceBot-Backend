namespace Application.EdgeIntegration;

public sealed class ExecutionReportIngestionOptions
{
    public const string SectionName = "ExecutionReportIngestion";

    public int MaxFutureClockSkewSeconds { get; set; } = 300;
}
