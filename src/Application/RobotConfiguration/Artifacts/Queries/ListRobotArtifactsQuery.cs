using Application.RobotConfiguration.Artifacts.Results;
using Application.RobotConfiguration.Artifacts.Commands;
using Application.Identity.Tokens.Claims;
using Domain.RobotConfiguration.Artifacts;

namespace Application.RobotConfiguration.Artifacts.Queries;

public sealed class ListRobotArtifactsQuery
{
    public required CurrentUserContext UserContext { get; init; }
    public Guid OrganizationId { get; init; }
    public string? Search { get; init; }
    public RobotArtifactStatus? Status { get; init; }
    public int PageNumber { get; init; } = 1;
    public int PageSize { get; init; } = 20;
}
