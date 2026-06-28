using Application.Identity.Tokens.Claims;

namespace Application.RobotConfiguration.Commands;

public sealed class RetireRobotArtifactCommand
{
    public required CurrentUserContext UserContext { get; init; }
    public Guid OrganizationId { get; init; }
    public Guid ArtifactId { get; init; }
}
