using Application.Identity.Tokens.Claims;

namespace Application.RobotConfiguration.ArtifactTemplates.Commands;

public sealed class UploadRobotArtifactTemplateCommand
{
    public required CurrentUserContext UserContext { get; init; }
    public required string FileName { get; init; }
    public string? ContentType { get; init; }
    public long ContentLengthBytes { get; init; }
    public required Stream Content { get; init; }
    public required string TemplateCode { get; init; }
    public required string TemplateName { get; init; }
    public required string RuntimeTargetCode { get; init; }
    public required string MachineModelCode { get; init; }
    public DateTimeOffset? ExportedAt { get; init; }
    public string? Description { get; init; }
    public string? MetadataJson { get; init; }
}

public sealed class BulkUploadRobotArtifactTemplatesCommand
{
    public required CurrentUserContext UserContext { get; init; }
    public IReadOnlyCollection<UploadRobotArtifactTemplateCommand> Items { get; init; } = [];
}
