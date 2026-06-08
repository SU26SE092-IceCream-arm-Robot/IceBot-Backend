using Application.Identity.Tokens.Claims;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;

namespace WebAPI.Authorization;

public static class ClaimsPrincipalExtensions
{
    public static CurrentUserContext GetUserContext(this ClaimsPrincipal principal)
    {
        var accountIdClaim = principal.FindFirstValue(ClaimTypes.NameIdentifier);
        _ = Guid.TryParse(accountIdClaim, out var accountId);

        var roles = principal.FindAll(ClaimTypes.Role).Select(c => c.Value).ToList();
        var isSystemAdmin = roles.Contains("SystemAdmin", StringComparer.OrdinalIgnoreCase);

        var allowedOrgIds = new HashSet<Guid>();
        var allowedStoreIds = new HashSet<Guid>();
        var roleScopes = principal.FindAll("role_scope").Select(c => c.Value).ToList();
        foreach (var scopeVal in roleScopes)
        {
            var parts = scopeVal.Split('|');
            if (parts.Length == 4)
            {
                var roleCode = parts[0];
                var orgIdStr = parts[1];
                var storeIdStr = parts[2];

                if (!IsManagementTenantRole(roleCode))
                {
                    continue;
                }

                if (orgIdStr != "*" &&
                    storeIdStr == "*" &&
                    Guid.TryParse(orgIdStr, out var orgId))
                {
                    allowedOrgIds.Add(orgId);
                }

                if (storeIdStr != "*" && Guid.TryParse(storeIdStr, out var storeId))
                {
                    allowedStoreIds.Add(storeId);
                }
            }
        }

        return new CurrentUserContext
        {
            AccountId = accountId,
            IsSystemAdmin = isSystemAdmin,
            AllowedOrganizationIds = allowedOrgIds,
            AllowedStoreIds = allowedStoreIds
        };
    }

    private static bool IsManagementTenantRole(string roleCode)
    {
        return roleCode.Equals("OrgAdmin", StringComparison.OrdinalIgnoreCase) ||
               roleCode.Equals("Manager", StringComparison.OrdinalIgnoreCase) ||
               roleCode.Equals("SystemAdmin", StringComparison.OrdinalIgnoreCase);
    }
}
