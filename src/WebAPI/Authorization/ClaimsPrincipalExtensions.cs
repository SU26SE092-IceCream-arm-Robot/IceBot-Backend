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
        var allowedKioskIds = new HashSet<Guid>();

        var allowedRoles = new[] { "OrgAdmin", "Manager", "Technician", "SystemAdmin" };
        var roleScopes = principal.FindAll("role_scope").Select(c => c.Value).ToList();

        foreach (var scopeVal in roleScopes)
        {
            var parts = scopeVal.Split('|');
            if (parts.Length == 4)
            {
                var roleCode = parts[0];
                if (!allowedRoles.Contains(roleCode, StringComparer.OrdinalIgnoreCase))
                {
                    continue;
                }

                var orgIdStr = parts[1];
                var storeIdStr = parts[2];
                var kioskIdStr = parts[3];

                if (kioskIdStr != "*" && Guid.TryParse(kioskIdStr, out var kioskId))
                {
                    allowedKioskIds.Add(kioskId);
                }
                else if (storeIdStr != "*" && Guid.TryParse(storeIdStr, out var storeId))
                {
                    allowedStoreIds.Add(storeId);
                }
                else if (orgIdStr != "*" && Guid.TryParse(orgIdStr, out var orgId))
                {
                    allowedOrgIds.Add(orgId);
                }
            }
        }

        return new CurrentUserContext
        {
            AccountId = accountId,
            IsSystemAdmin = isSystemAdmin,
            AllowedOrganizationIds = allowedOrgIds,
            AllowedStoreIds = allowedStoreIds,
            AllowedKioskIds = allowedKioskIds
        };
    }
}
