using Application.Identity.Tokens.Claims;

namespace Application.RobotConfiguration.Queries;

public sealed record GetRobotProgramQuery(Guid ProgramId)
{
    public required CurrentUserContext UserContext { get; init; }
}
