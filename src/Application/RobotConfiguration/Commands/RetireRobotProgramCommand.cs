using Application.Identity.Tokens.Claims;

namespace Application.RobotConfiguration.Commands;

public sealed class RetireRobotProgramCommand
{
    public required CurrentUserContext UserContext { get; init; }
    public Guid ProgramId { get; init; }
}
