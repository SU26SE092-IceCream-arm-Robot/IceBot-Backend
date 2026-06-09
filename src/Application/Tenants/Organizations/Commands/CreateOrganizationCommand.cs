using Application.Identity.Tokens.Claims;
using Application.Tenants.Organizations.Requests;

namespace Application.Tenants.Organizations.Commands;

public sealed class CreateOrganizationCommand
{
    public CurrentUserContext UserContext { get; init; } = null!;
    public CreateOrganizationRequest Request { get; init; } = null!;
}
