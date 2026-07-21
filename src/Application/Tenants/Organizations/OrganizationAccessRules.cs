using Application.Identity.Tokens.Claims;

namespace Application.Tenants.Organizations;

internal static class OrganizationAccessRules
{
    public static bool CanAccessOrganization(
        IReadOnlyCollection<string> allowedRoles,
        CurrentUserContext userContext,
        Guid organizationId) =>
        ScopeAccessRules.CanAccessScopedRow(allowedRoles, userContext, organizationId, null, null);

    public static bool CanManageOrganizationLifecycle(CurrentUserContext userContext)
    {
        return userContext.IsSystemAdmin;
    }
}
