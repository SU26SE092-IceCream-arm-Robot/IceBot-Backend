namespace Application.RobotConfiguration.Results;

public sealed class RobotArtifactReviewUrlResult
{
    public Guid RobotArtifactId { get; init; }
    public string FileName { get; init; } = null!;
    public string Checksum { get; init; } = null!;
    public long ContentLengthBytes { get; init; }
    public string Url { get; init; } = null!;
    public DateTimeOffset ExpiresAt { get; init; }
}

public sealed class RobotArtifactDiscardResult
{
    public Guid RobotArtifactId { get; init; }
    public string FileName { get; init; } = null!;
    public bool ObjectDeleted { get; init; }
}
