using Application.Identity.Tokens.Claims;

namespace Application.Operations.MaintenanceTickets.Rules;

public static class MaintenanceTicketAccessRules
{
    public static bool CanView(CurrentUserContext user, Guid orgId, Guid storeId, Guid kioskId, Guid? assignedToAccountId)
    {
        if (user.IsSystemAdmin) return true;

        foreach (var rs in user.RoleScopes)
        {
            if (rs.OrganizationId == orgId || rs.StoreId == storeId || rs.KioskId == kioskId ||
                (rs.OrganizationId == null && rs.StoreId == null && rs.KioskId == null))
            {
                if (string.Equals(rs.RoleCode, "SystemAdmin", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(rs.RoleCode, "OrgAdmin", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(rs.RoleCode, "Manager", StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }

                if (string.Equals(rs.RoleCode, "Staff", StringComparison.OrdinalIgnoreCase))
                {
                    if (rs.KioskId == kioskId || rs.StoreId == storeId || rs.OrganizationId == orgId)
                    {
                        return true;
                    }
                }

                if (string.Equals(rs.RoleCode, "Technician", StringComparison.OrdinalIgnoreCase))
                {
                    if (assignedToAccountId == user.AccountId ||
                        rs.KioskId == kioskId || rs.StoreId == storeId || rs.OrganizationId == orgId)
                    {
                        return true;
                    }
                }
            }
        }

        if (user.AllowedOrganizationIds.Contains(orgId) ||
            user.AllowedStoreIds.Contains(storeId) ||
            user.AllowedKioskIds.Contains(kioskId))
        {
            return true;
        }

        return false;
    }

    public static bool CanCreate(CurrentUserContext user, Guid orgId, Guid storeId, Guid kioskId)
    {
        if (user.IsSystemAdmin) return true;

        foreach (var rs in user.RoleScopes)
        {
            var isOrgMatch = rs.OrganizationId == null || rs.OrganizationId == orgId;
            var isStoreMatch = rs.StoreId == null || rs.StoreId == storeId;
            var isKioskMatch = rs.KioskId == null || rs.KioskId == kioskId;

            if (isOrgMatch && isStoreMatch && isKioskMatch)
            {
                return true;
            }
        }

        if (user.AllowedOrganizationIds.Contains(orgId) ||
            user.AllowedStoreIds.Contains(storeId) ||
            user.AllowedKioskIds.Contains(kioskId))
        {
            return true;
        }

        return false;
    }

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

        if (user.AllowedOrganizationIds.Contains(orgId) ||
            user.AllowedStoreIds.Contains(storeId) ||
            user.AllowedKioskIds.Contains(kioskId))
        {
            return true;
        }

        return false;
    }
}
