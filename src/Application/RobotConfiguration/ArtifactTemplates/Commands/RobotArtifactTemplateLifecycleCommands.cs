using Application.Identity.Tokens.Claims;

namespace Application.RobotConfiguration.ArtifactTemplates.Commands;

public sealed record PublishRobotArtifactTemplateCommand(Guid TemplateId) { public required CurrentUserContext UserContext { get; init; } }
public sealed record RetireRobotArtifactTemplateCommand(Guid TemplateId) { public required CurrentUserContext UserContext { get; init; } }
public sealed record DiscardDraftRobotArtifactTemplateCommand(Guid TemplateId) { public required CurrentUserContext UserContext { get; init; } }

public sealed class CloneRobotArtifactTemplateCommand
{
    public required CurrentUserContext UserContext { get; init; }
    public Guid OrganizationId { get; init; }
    public Guid TemplateId { get; init; }
    public required string ArtifactCode { get; init; }
    public required string ArtifactName { get; init; }
    public string? Description { get; init; }
    public string? MetadataJson { get; init; }
}
