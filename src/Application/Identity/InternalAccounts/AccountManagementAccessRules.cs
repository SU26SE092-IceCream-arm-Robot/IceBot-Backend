using Application.Identity.Tokens.Claims;
using Application.Tenants;
using Domain.Identity.Entities;

namespace Application.Identity.InternalAccounts;

internal static class AccountManagementAccessRules
{
    private static readonly string[] ManagingRoles = ["SystemAdmin", "OrgAdmin"];

    public static bool CanReadAccount(CurrentUserContext userContext, Guid organizationId, Account account)
    {
        if (userContext.IsSystemAdmin)
        {
            return account.AccountRoles.Any(role => role.IsActive && BelongsToOrganization(role, organizationId));
        }

        return account.AccountRoles.Any(role => role.IsActive &&
            BelongsToOrganization(role, organizationId) &&
            ScopeAccessRules.CanAccessScopedRow(
                ManagingRoles,
                userContext,
                role.OrganizationId,
                role.StoreId,
                role.KioskId));
    }

    public static bool CanManageAccount(CurrentUserContext userContext, Guid organizationId, Account account)
    {
        if (userContext.IsSystemAdmin)
        {
            return account.AccountRoles.Any(role => role.IsActive && BelongsToOrganization(role, organizationId));
        }

        var activeRoles = account.AccountRoles.Where(role => role.IsActive).ToArray();
        return activeRoles.Length > 0 && activeRoles.All(role =>
            BelongsToOrganization(role, organizationId) &&
            ScopeAccessRules.CanAccessScopedRow(
                ManagingRoles,
                userContext,
                role.OrganizationId,
                role.StoreId,
                role.KioskId));
    }

    public static bool BelongsToOrganization(AccountRole role, Guid organizationId)
    {
        var hasScope = role.OrganizationId.HasValue || role.StoreId.HasValue || role.KioskId.HasValue;
        return hasScope &&
            (!role.OrganizationId.HasValue || role.OrganizationId == organizationId) &&
            (role.Store is null || role.Store.OrganizationId == organizationId) &&
            (role.Kiosk is null || role.Kiosk.OrganizationId == organizationId);
    }
}
