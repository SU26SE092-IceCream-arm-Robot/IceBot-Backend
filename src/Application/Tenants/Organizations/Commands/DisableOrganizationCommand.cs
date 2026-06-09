using Application.Identity.Tokens.Claims;

namespace Application.Tenants.Organizations.Commands;

public sealed class DisableOrganizationCommand
{
    public CurrentUserContext UserContext { get; init; } = null!;
    public Guid OrganizationId { get; init; }
}
