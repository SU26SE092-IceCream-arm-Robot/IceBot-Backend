using Application.RobotConfiguration.Artifacts.Abstractions;
using Application.Identity.Tokens.Claims;

namespace Application.RobotConfiguration.Programs.Commands;

public sealed class PublishRobotProgramCommand
{
    public required CurrentUserContext UserContext { get; init; }
    public required Guid OrganizationId { get; init; }
    public required Guid ProgramId { get; init; }
}
