namespace Application.RobotConfiguration.Results;

public sealed class BulkRobotArtifactPublishResult
{
    public int TotalCount { get; init; }
    public int PublishedCount { get; init; }
    public int AlreadyPublishedCount { get; init; }
    public IReadOnlyCollection<BulkRobotArtifactPublishItemResult> Items { get; init; } = Array.Empty<BulkRobotArtifactPublishItemResult>();
}

public sealed class BulkRobotArtifactPublishItemResult
{
    public Guid RobotArtifactId { get; init; }
    public string ArtifactCode { get; init; } = string.Empty;
    public string FileName { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public bool WasAlreadyPublished { get; init; }
}
