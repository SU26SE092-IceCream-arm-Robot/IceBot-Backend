using Application.Identity.Tokens.Claims;

namespace Application.RobotConfiguration.Commands;

public sealed class BulkPublishRobotArtifactsCommand
{
    public required CurrentUserContext UserContext { get; init; }
    public Guid OrganizationId { get; init; }
    public IReadOnlyCollection<Guid> RobotArtifactIds { get; init; } = Array.Empty<Guid>();
}
