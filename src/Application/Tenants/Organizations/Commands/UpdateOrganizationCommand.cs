using Application.Identity.Tokens.Claims;
using Application.Tenants.Organizations.Requests;

namespace Application.Tenants.Organizations.Commands;

public sealed class UpdateOrganizationCommand
{
    public CurrentUserContext UserContext { get; init; } = null!;
    public Guid OrganizationId { get; init; }
    public UpdateOrganizationRequest Request { get; init; } = null!;
}
