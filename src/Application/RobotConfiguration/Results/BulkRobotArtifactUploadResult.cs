namespace Application.RobotConfiguration.Results;

public sealed class BulkRobotArtifactUploadResult
{
    public int TotalCount { get; init; }
    public int SucceededCount { get; init; }
    public int FailedCount { get; init; }
    public IReadOnlyCollection<BulkRobotArtifactUploadItemResult> Items { get; init; } = Array.Empty<BulkRobotArtifactUploadItemResult>();
}

public sealed class BulkRobotArtifactUploadItemResult
{
    public string FileName { get; init; } = string.Empty;
    public bool Succeeded { get; init; }
    public int StatusCode { get; init; }
    public string? Message { get; init; }
    public Guid? RobotArtifactId { get; init; }
    public RobotArtifactResult? Artifact { get; init; }
}
