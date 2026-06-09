using Domain.Tenants.Enums;
using System;
using System.Collections.Generic;

namespace Application.Identity.Roles.Rules;

internal static class RoleCatalogRules
{
    public static readonly Dictionary<string, (string Description, TenantScopeType[] AllowedScopes, bool RequiresScope)> RoleMetadata = new(StringComparer.OrdinalIgnoreCase)
    {
        ["SystemAdmin"] = ("System Admin. Full platform-wide administrative control.", new[] { TenantScopeType.Global }, false),
        ["OrgAdmin"] = ("Organization Admin. Manage assigned organization resources.", new[] { TenantScopeType.Organization }, true),
        ["Manager"] = ("Manager. Operations management across organizations and stores.", new[] { TenantScopeType.Organization, TenantScopeType.Store }, true),
        ["Technician"] = ("Technician. Installation, maintenance, and robot/kiosk configuration.", new[] { TenantScopeType.Store, TenantScopeType.Kiosk }, true),
        ["Staff"] = ("Staff. On-site kiosk operations.", new[] { TenantScopeType.Store, TenantScopeType.Kiosk }, true)
    };

    public static bool CanAssignRole(string? userRoleCode, string targetRoleCode)
    {
        if (string.IsNullOrWhiteSpace(userRoleCode))
        {
            return false;
        }

        if (string.Equals(userRoleCode, "SystemAdmin", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (string.Equals(userRoleCode, "OrgAdmin", StringComparison.OrdinalIgnoreCase))
        {
            return string.Equals(targetRoleCode, "Manager", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(targetRoleCode, "Staff", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(targetRoleCode, "Technician", StringComparison.OrdinalIgnoreCase);
        }

        if (string.Equals(userRoleCode, "Manager", StringComparison.OrdinalIgnoreCase))
        {
            return string.Equals(targetRoleCode, "Staff", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(targetRoleCode, "Technician", StringComparison.OrdinalIgnoreCase);
        }

        return false;
    }
}
