using Application.Identity.Tokens.Claims;
using Application.Tenants;

namespace Application.Operations.MaintenanceTickets.Rules;

public static class MaintenanceTicketAccessRules
{
    public static bool CanView(CurrentUserContext user, Guid orgId, Guid storeId, Guid kioskId, Guid? assignedToAccountId)
        => ScopeAccessRules.CanAccessScopedRow(
            ScopeRoleSets.MaintenanceView, user, orgId, storeId, kioskId);

    public static bool CanCreate(CurrentUserContext user, Guid orgId, Guid storeId, Guid kioskId)
        => ScopeAccessRules.CanAccessScopedRow(
            ScopeRoleSets.MaintenanceCreate, user, orgId, storeId, kioskId);

    public static bool CanAssign(CurrentUserContext user, Guid orgId, Guid storeId, Guid kioskId)
    {
        if (user.IsSystemAdmin) return true;

        foreach (var rs in user.RoleScopes)
        {
            var isRoleAllowed = string.Equals(rs.RoleCode, "OrgAdmin", StringComparison.OrdinalIgnoreCase) ||
                                string.Equals(rs.RoleCode, "Manager", StringComparison.OrdinalIgnoreCase);

            if (isRoleAllowed)
            {
                var isOrgMatch = rs.OrganizationId == null || rs.OrganizationId == orgId;
                var isStoreMatch = rs.StoreId == null || rs.StoreId == storeId;
                var isKioskMatch = rs.KioskId == null || rs.KioskId == kioskId;

                if (isOrgMatch && isStoreMatch && isKioskMatch)
                {
                    return true;
                }
            }
        }

        return false;
    }

    public static bool CanStart(CurrentUserContext user, Guid orgId, Guid storeId, Guid kioskId)
    {
        if (user.IsSystemAdmin) return true;

        foreach (var rs in user.RoleScopes)
        {
            var isRoleAllowed = string.Equals(rs.RoleCode, "OrgAdmin", StringComparison.OrdinalIgnoreCase) ||
                                string.Equals(rs.RoleCode, "Manager", StringComparison.OrdinalIgnoreCase) ||
                                string.Equals(rs.RoleCode, "Technician", StringComparison.OrdinalIgnoreCase);

            if (isRoleAllowed)
            {
                var isOrgMatch = rs.OrganizationId == null || rs.OrganizationId == orgId;
                var isStoreMatch = rs.StoreId == null || rs.StoreId == storeId;
                var isKioskMatch = rs.KioskId == null || rs.KioskId == kioskId;

                if (isOrgMatch && isStoreMatch && isKioskMatch)
                {
                    return true;
                }
            }
        }

        return false;
    }

    public static bool CanResolve(CurrentUserContext user, Guid orgId, Guid storeId, Guid kioskId, Guid? assignedToAccountId)
    {
        if (user.IsSystemAdmin) return true;

        foreach (var rs in user.RoleScopes)
        {
            var isManagerOrAdmin = string.Equals(rs.RoleCode, "OrgAdmin", StringComparison.OrdinalIgnoreCase) ||
                                  string.Equals(rs.RoleCode, "Manager", StringComparison.OrdinalIgnoreCase);

            var isOrgMatch = rs.OrganizationId == null || rs.OrganizationId == orgId;
            var isStoreMatch = rs.StoreId == null || rs.StoreId == storeId;
            var isKioskMatch = rs.KioskId == null || rs.KioskId == kioskId;

            if (isManagerOrAdmin && isOrgMatch && isStoreMatch && isKioskMatch)
            {
                return true;
            }

            if (string.Equals(rs.RoleCode, "Technician", StringComparison.OrdinalIgnoreCase))
            {
                if (assignedToAccountId == user.AccountId || (isOrgMatch && isStoreMatch && isKioskMatch))
                {
                    return true;
                }
            }
        }

        return false;
    }

    public static bool CanClose(CurrentUserContext user, Guid orgId, Guid storeId, Guid kioskId)
    {
        if (user.IsSystemAdmin) return true;

        foreach (var rs in user.RoleScopes)
        {
            var isRoleAllowed = string.Equals(rs.RoleCode, "OrgAdmin", StringComparison.OrdinalIgnoreCase) ||
                                string.Equals(rs.RoleCode, "Manager", StringComparison.OrdinalIgnoreCase);

            if (isRoleAllowed)
            {
                var isOrgMatch = rs.OrganizationId == null || rs.OrganizationId == orgId;
                var isStoreMatch = rs.StoreId == null || rs.StoreId == storeId;
                var isKioskMatch = rs.KioskId == null || rs.KioskId == kioskId;

                if (isOrgMatch && isStoreMatch && isKioskMatch)
                {
                    return true;
                }
            }
        }

        return false;
    }

    public static bool CanCancel(CurrentUserContext user, Guid orgId, Guid storeId, Guid kioskId)
    {
        if (user.IsSystemAdmin) return true;

        foreach (var rs in user.RoleScopes)
        {
            var isRoleAllowed = string.Equals(rs.RoleCode, "OrgAdmin", StringComparison.OrdinalIgnoreCase) ||
                                string.Equals(rs.RoleCode, "Manager", StringComparison.OrdinalIgnoreCase);

            if (isRoleAllowed)
            {
                var isOrgMatch = rs.OrganizationId == null || rs.OrganizationId == orgId;
                var isStoreMatch = rs.StoreId == null || rs.StoreId == storeId;
                var isKioskMatch = rs.KioskId == null || rs.KioskId == kioskId;

                if (isOrgMatch && isStoreMatch && isKioskMatch)
                {
                    return true;
                }
            }
        }

        return false;
    }

    public static bool CanUpdate(CurrentUserContext user, Guid orgId, Guid storeId, Guid kioskId)
        => ScopeAccessRules.CanAccessScopedRow(
            ScopeRoleSets.MaintenanceManage, user, orgId, storeId, kioskId);
}
