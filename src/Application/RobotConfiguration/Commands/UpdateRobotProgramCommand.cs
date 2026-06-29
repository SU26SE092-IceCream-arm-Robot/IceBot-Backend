using Application.Identity.Tokens.Claims;

namespace Application.RobotConfiguration.Commands;

public sealed class UpdateRobotProgramCommand
{
    public required CurrentUserContext UserContext { get; init; }
    public Guid OrganizationId { get; init; }
    public Guid ProgramId { get; init; }
    public string Code { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
}
