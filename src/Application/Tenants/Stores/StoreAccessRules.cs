using Application.Identity.Tokens.Claims;
using Domain.Tenants.Entities;

namespace Application.Tenants.Stores;

internal static class StoreAccessRules
{
    public static bool CanAccessStore(
        IReadOnlyCollection<string> allowedRoles,
        CurrentUserContext userContext,
        Store store) =>
        ScopeAccessRules.CanAccessScopedRow(
            allowedRoles, userContext, store.OrganizationId, store.Id, null);

    public static bool CanManageOrganizationStores(
        IReadOnlyCollection<string> allowedRoles,
        CurrentUserContext userContext,
        Guid organizationId) =>
        ScopeAccessRules.CanAccessScopedRow(
            allowedRoles, userContext, organizationId, null, null);
}
