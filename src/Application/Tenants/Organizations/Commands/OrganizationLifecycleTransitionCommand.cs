using Application.Identity.Tokens.Claims;
using Application.Tenants.Organizations.Requests;

namespace Application.Tenants.Organizations.Commands;

public enum OrganizationLifecycleAction
{
    Suspend,
    Resume,
    Deactivate,
    Reactivate
}

public sealed class OrganizationLifecycleTransitionCommand
{
    public CurrentUserContext UserContext { get; init; } = null!;

    public Guid OrganizationId { get; init; }

    public OrganizationLifecycleAction Action { get; init; }

    public OrganizationLifecycleTransitionRequest Request { get; init; } = null!;
}
