using Application.Identity.InternalAccounts.Requests;
using Application.Identity.Roles.Rules;
using Application.Identity.Tokens.Claims;
using Application.Tenants;
using Domain.Tenants.Enums;

namespace Application.Identity.InternalAccounts;

internal static class AccountRoleAssignmentRules
{
    public static string? ValidateRoleAssignmentPermission(
        CurrentUserContext userContext,
        IReadOnlyCollection<string> userRoles,
        string targetRoleCode)
    {
        if (userContext.IsSystemAdmin ||
            userRoles.Contains("SystemAdmin", StringComparer.OrdinalIgnoreCase))
        {
            return null;
        }

        var canAssign = userRoles.Any(roleCode => RoleCatalogRules.CanAssignRole(roleCode, targetRoleCode));
        return canAssign
            ? null
            : "Current account is not allowed to assign this role.";
    }

    public static string? ValidateRequestedScope(
        CurrentUserContext userContext,
        string targetRoleCode,
        AccountRoleScopeRequest request)
    {
        if (!RoleCatalogRules.RoleMetadata.TryGetValue(targetRoleCode, out var metadata))
        {
            return "Role scope metadata is not configured.";
        }

        var selectedScope = ResolveSelectedScope(request);
        if (!metadata.RequiresScope)
        {
            return selectedScope == TenantScopeType.Global
                ? null
                : "This role does not accept organization, store, or kiosk scope.";
        }

        if (selectedScope == TenantScopeType.Global)
        {
            return "This role requires an organization, store, or kiosk scope.";
        }

        if (!metadata.AllowedScopes.Contains(selectedScope))
        {
            return $"Role '{targetRoleCode}' does not allow {selectedScope} scope.";
        }

        if (userContext.IsSystemAdmin)
        {
            return null;
        }

        var assigningRoles = userContext.RoleScopes
            .Select(scope => scope.RoleCode)
            .Where(roleCode => RoleCatalogRules.CanAssignRole(roleCode, targetRoleCode))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (assigningRoles.Length == 0)
        {
            return "Current account is not allowed to assign this role.";
        }

        return ScopeAccessRules.CanAccessScopedRow(
            assigningRoles,
            userContext,
            request.OrganizationId,
            request.StoreId,
            request.KioskId)
            ? null
            : $"Current account is not allowed to assign this {selectedScope.ToString().ToLowerInvariant()} scope.";
    }

    public static TenantScopeType ResolveSelectedScope(AccountRoleScopeRequest request)
    {
        if (request.KioskId.HasValue)
        {
            return TenantScopeType.Kiosk;
        }

        if (request.StoreId.HasValue)
        {
            return TenantScopeType.Store;
        }

        return request.OrganizationId.HasValue
            ? TenantScopeType.Organization
            : TenantScopeType.Global;
    }

}
