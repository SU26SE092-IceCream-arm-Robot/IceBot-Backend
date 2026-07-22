using Application.RobotConfiguration.Artifacts.Abstractions;
using Application.Identity.Tokens.Claims;

namespace Application.RobotConfiguration.Programs.Commands;

public sealed class CreateRobotProgramCommand
{
    public required CurrentUserContext UserContext { get; init; }
    public Guid OrganizationId { get; init; }
    public Guid? StoreId { get; init; }
    public Guid? KioskId { get; init; }
    public Guid? DeviceId { get; init; }
    public string Code { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
}
