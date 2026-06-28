using Application.Identity.Tokens.Claims;

namespace Application.RobotConfiguration.Commands;

public sealed class BulkUploadRobotArtifactsCommand
{
    public required CurrentUserContext UserContext { get; init; }
    public Guid OrganizationId { get; init; }
    public IReadOnlyCollection<BulkUploadRobotArtifactItem> Items { get; init; } = Array.Empty<BulkUploadRobotArtifactItem>();
}

public sealed class BulkUploadRobotArtifactItem
{
    public string FileName { get; init; } = string.Empty;
    public string ContentType { get; init; } = string.Empty;
    public long ContentLengthBytes { get; init; }
    public required Stream Content { get; init; }
    public string ArtifactCode { get; init; } = string.Empty;
    public string ArtifactName { get; init; } = string.Empty;
    public string RuntimeTargetCode { get; init; } = string.Empty;
    public string MachineModelCode { get; init; } = string.Empty;
    public DateTimeOffset? ExportedAt { get; init; }
    public string? Description { get; init; }
    public string? MetadataJson { get; init; }
}
