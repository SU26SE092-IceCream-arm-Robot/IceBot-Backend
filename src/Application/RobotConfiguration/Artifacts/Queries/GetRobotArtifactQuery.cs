using Application.Identity.Tokens.Claims;

namespace Application.RobotConfiguration.Artifacts.Queries;

public sealed record GetRobotArtifactQuery(Guid OrganizationId, Guid ArtifactId)
{
    public required CurrentUserContext UserContext { get; init; }
}
