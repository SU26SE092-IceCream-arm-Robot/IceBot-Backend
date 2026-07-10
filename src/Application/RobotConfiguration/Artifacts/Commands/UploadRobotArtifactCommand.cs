using Application.Identity.Tokens.Claims;

namespace Application.RobotConfiguration.Artifacts.Commands;

public sealed class UploadRobotArtifactCommand
{
    public required CurrentUserContext UserContext { get; init; }
    public required Guid OrganizationId { get; init; }
    public required string ArtifactCode { get; init; }
    public required string ArtifactName { get; init; }
    public required string FileName { get; init; }
    public required string RuntimeTargetCode { get; init; }
    public required string MachineModelCode { get; init; }
    public required string ContentType { get; init; }
    public required long ContentLengthBytes { get; init; }
    public required Stream Content { get; init; }
    public DateTimeOffset? ExportedAt { get; init; }
    public string? Description { get; init; }
    public string? MetadataJson { get; init; }
}
