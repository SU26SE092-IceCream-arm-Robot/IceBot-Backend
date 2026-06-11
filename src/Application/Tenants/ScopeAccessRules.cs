using Application.Identity.Tokens.Claims;
using Domain.Identity.Entities;

namespace Application.Tenants;

public static class ScopeAccessRules
{
    public static bool CanAccessScopedRow(
        CurrentUserContext userContext,
        Guid? organizationId,
        Guid? storeId,
        Guid? kioskId)
    {
        if (userContext.IsSystemAdmin)
        {
            return true;
        }

        if (organizationId.HasValue && userContext.AllowedOrganizationIds.Contains(organizationId.Value))
        {
            return true;
        }

        if (storeId.HasValue && userContext.AllowedStoreIds.Contains(storeId.Value))
        {
            return true;
        }

        if (kioskId.HasValue && userContext.AllowedKioskIds.Contains(kioskId.Value))
        {
            return true;
        }

        return false;
    }

    public static bool SharesAnyActiveScope(CurrentUserContext userContext, IEnumerable<AccountRole> roles)
    {
        if (userContext.IsSystemAdmin)
        {
            return true;
        }

        return roles.Any(role =>
            role.IsActive &&
            (
                (role.OrganizationId.HasValue && userContext.AllowedOrganizationIds.Contains(role.OrganizationId.Value)) ||
                (role.StoreId.HasValue && userContext.AllowedStoreIds.Contains(role.StoreId.Value)) ||
                (role.KioskId.HasValue && userContext.AllowedKioskIds.Contains(role.KioskId.Value))
            )
        );
    }
}
