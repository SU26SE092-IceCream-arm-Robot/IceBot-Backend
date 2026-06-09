using Application.Identity.Tokens.Claims;
using Domain.Tenants.Entities;

namespace Application.Tenants.Kiosks;

internal static class KioskAccessRules
{
    public static bool CanAccessKiosk(CurrentUserContext userContext, Kiosk kiosk)
    {
        return userContext.IsSystemAdmin
            || userContext.AllowedOrganizationIds.Contains(kiosk.OrganizationId)
            || userContext.AllowedStoreIds.Contains(kiosk.StoreId)
            || userContext.AllowedKioskIds.Contains(kiosk.Id);
    }

    public static bool CanManageStoreKiosks(CurrentUserContext userContext, Store store)
    {
        return userContext.IsSystemAdmin
            || userContext.AllowedOrganizationIds.Contains(store.OrganizationId)
            || userContext.AllowedStoreIds.Contains(store.Id);
    }
}
