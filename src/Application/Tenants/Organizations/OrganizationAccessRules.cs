using Application.Identity.Tokens.Claims;

namespace Application.Tenants.Organizations;

internal static class OrganizationAccessRules
{
    public static bool CanAccessOrganization(CurrentUserContext userContext, Guid organizationId)
    {
        return userContext.IsSystemAdmin || userContext.AllowedOrganizationIds.Contains(organizationId);
    }

    public static bool CanManageOrganizationLifecycle(CurrentUserContext userContext)
    {
        return userContext.IsSystemAdmin;
    }
}
