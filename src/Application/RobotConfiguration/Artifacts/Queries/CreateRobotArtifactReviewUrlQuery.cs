using Application.Identity.Tokens.Claims;

namespace Application.RobotConfiguration.Artifacts.Queries;

public sealed class CreateRobotArtifactReviewUrlQuery
{
    public required CurrentUserContext UserContext { get; init; }
    public Guid OrganizationId { get; init; }
    public Guid ArtifactId { get; init; }
}
