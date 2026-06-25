using Application.Identity.Tokens.Claims;

namespace Application.RobotConfiguration.Commands;

public sealed class PublishRobotArtifactCommand
{
    public required CurrentUserContext UserContext { get; init; }
    public required Guid ArtifactId { get; init; }
}
