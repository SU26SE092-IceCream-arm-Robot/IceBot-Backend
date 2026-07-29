using Application.RobotConfiguration.Artifacts.Abstractions;
using Application.Identity.Tokens.Claims;

namespace Application.RobotConfiguration.Programs.Queries;

public sealed record GetRobotProgramQuery(Guid OrganizationId, Guid ProgramId)
{
    public required CurrentUserContext UserContext { get; init; }
}
