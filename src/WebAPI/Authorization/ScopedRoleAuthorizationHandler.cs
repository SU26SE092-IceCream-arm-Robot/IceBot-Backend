using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace WebAPI.Authorization;

public sealed class ScopedRoleAuthorizationHandler : AuthorizationHandler<ScopedRoleRequirement>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        ScopedRoleRequirement requirement)
    {
        // Current policies only require role presence. Route/resource scope matching will be added
        // when APIs start passing OrganizationId/StoreId/KioskId authorization context.
        var roleScopes = context.User.FindAll("role_scope")
            .Select(claim => AccountRoleScopeClaim.TryParse(claim.Value))
            .Where(scope => scope is not null)
            .Select(scope => scope!)
            .ToList();

        var hasScopedRole = roleScopes.Any(scope =>
            requirement.AllowedRoles.Contains(scope.RoleCode, StringComparer.OrdinalIgnoreCase));

        var hasGlobalSystemAdminRole = requirement.AllowedRoles.Contains("SystemAdmin", StringComparer.OrdinalIgnoreCase) &&
            context.User.FindAll(ClaimTypes.Role)
                .Any(claim => string.Equals(claim.Value, "SystemAdmin", StringComparison.OrdinalIgnoreCase));

        if (hasScopedRole || hasGlobalSystemAdminRole)
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }

    private sealed record AccountRoleScopeClaim(
        string RoleCode,
        string OrganizationId,
        string StoreId,
        string KioskId)
    {
        public static AccountRoleScopeClaim? TryParse(string value)
        {
            var parts = value.Split('|');
            return parts.Length == 4
                ? new AccountRoleScopeClaim(parts[0], parts[1], parts[2], parts[3])
                : null;
        }
    }
}
