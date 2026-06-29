using Application.Identity.Tokens.Claims;

namespace Application.RobotConfiguration.Commands;

public sealed class PublishRobotProgramCommand
{
    public required CurrentUserContext UserContext { get; init; }
    public required Guid OrganizationId { get; init; }
    public required Guid ProgramId { get; init; }
}
