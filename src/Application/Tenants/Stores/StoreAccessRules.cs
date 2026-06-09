using Application.Identity.Tokens.Claims;
using Domain.Tenants.Entities;

namespace Application.Tenants.Stores;

internal static class StoreAccessRules
{
    public static bool CanAccessStore(CurrentUserContext userContext, Store store)
    {
        return userContext.IsSystemAdmin ||
               userContext.AllowedOrganizationIds.Contains(store.OrganizationId) ||
               userContext.AllowedStoreIds.Contains(store.Id);
    }

    public static bool CanManageOrganizationStores(CurrentUserContext userContext, Guid organizationId)
    {
        return userContext.IsSystemAdmin ||
               userContext.AllowedOrganizationIds.Contains(organizationId);
    }
}
