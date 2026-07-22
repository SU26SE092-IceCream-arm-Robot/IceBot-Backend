using Application.Identity.Tokens.Claims;

namespace Application.RobotConfiguration.Artifacts.Commands;

public sealed class PublishRobotArtifactCommand
{
    public required CurrentUserContext UserContext { get; init; }
    public required Guid OrganizationId { get; init; }
    public required Guid ArtifactId { get; init; }
}
