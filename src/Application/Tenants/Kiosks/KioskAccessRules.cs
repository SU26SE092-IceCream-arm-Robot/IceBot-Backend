using Application.Identity.Tokens.Claims;
using Domain.Tenants.Entities;

namespace Application.Tenants.Kiosks;

internal static class KioskAccessRules
{
    public static bool CanAccessKiosk(
        IReadOnlyCollection<string> allowedRoles,
        CurrentUserContext userContext,
        Kiosk kiosk) =>
        ScopeAccessRules.CanAccessScopedRow(
            allowedRoles, userContext, kiosk.OrganizationId, kiosk.StoreId, kiosk.Id);

    public static bool CanManageStoreKiosks(
        IReadOnlyCollection<string> allowedRoles,
        CurrentUserContext userContext,
        Store store) =>
        ScopeAccessRules.CanAccessScopedRow(
            allowedRoles, userContext, store.OrganizationId, store.Id, null);
}
