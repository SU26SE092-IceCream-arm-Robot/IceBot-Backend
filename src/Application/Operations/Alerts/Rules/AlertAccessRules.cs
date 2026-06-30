using Application.Identity.Tokens.Claims;

namespace Application.Operations.Alerts.Rules;

public static class AlertAccessRules
{
    public static bool CanAccess(CurrentUserContext user, Guid organizationId, Guid storeId, Guid kioskId) =>
        user.IsSystemAdmin ||
        user.AllowedOrganizationIds.Contains(organizationId) ||
        user.AllowedStoreIds.Contains(storeId) ||
        user.AllowedKioskIds.Contains(kioskId);
}
