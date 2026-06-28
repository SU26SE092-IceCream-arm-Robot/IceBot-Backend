using Application.Identity.Tokens.Claims;
using Domain.Tenants.Enums;

namespace Application.RobotConfiguration.Commands;

public sealed class CreateRobotProgramCommand
{
    public required CurrentUserContext UserContext { get; init; }
    public Guid OrganizationId { get; init; }
    public Guid? StoreId { get; init; }
    public Guid? KioskId { get; init; }
    public Guid? DeviceId { get; init; }
    public TenantScopeType ScopeType { get; init; } = TenantScopeType.Organization;
    public string Code { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
}
