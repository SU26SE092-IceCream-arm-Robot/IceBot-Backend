using Application.Identity.Tokens.Claims;
using Application.Tenants;

namespace Application.Operations.Alerts.Rules;

public static class AlertAccessRules
{
    public static bool CanAccess(
        IReadOnlyCollection<string> allowedRoles,
        CurrentUserContext user,
        Guid organizationId,
        Guid storeId,
        Guid kioskId) =>
        ScopeAccessRules.CanAccessScopedRow(allowedRoles, user, organizationId, storeId, kioskId);
}
