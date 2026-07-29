using Application.RobotConfiguration.Artifacts.Queries;
using Application.RobotConfiguration.Artifacts.Commands;
using Domain.RobotConfiguration.Artifacts;
namespace Application.RobotConfiguration.Artifacts.Results;

public sealed class BulkRobotArtifactUploadResult
{
    public int TotalCount { get; init; }
    public int SucceededCount { get; init; }
    public int FailedCount { get; init; }
    public int UploadedCount { get; init; }
    public int ExistingCount { get; init; }
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
    public bool WasExisting { get; init; }
}
