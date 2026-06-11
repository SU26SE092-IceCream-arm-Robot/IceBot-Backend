using Application.Identity.InternalAccounts.Requests;
using Application.Identity.Roles.Rules;
using Application.Identity.Tokens.Claims;
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

        return selectedScope switch
        {
            TenantScopeType.Organization => request.OrganizationId.HasValue &&
                                            userContext.AllowedOrganizationIds.Contains(request.OrganizationId.Value)
                ? null
                : "Current account is not allowed to assign this organization scope.",

            TenantScopeType.Store => IsStoreScopeAllowed(userContext, request)
                ? null
                : "Current account is not allowed to assign this store scope.",

            TenantScopeType.Kiosk => IsKioskScopeAllowed(userContext, request)
                ? null
                : "Current account is not allowed to assign this kiosk scope.",

            _ => "Unsupported role scope."
        };
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

    private static bool IsStoreScopeAllowed(
        CurrentUserContext userContext,
        AccountRoleScopeRequest request)
    {
        if (request.StoreId.HasValue && userContext.AllowedStoreIds.Contains(request.StoreId.Value))
        {
            return true;
        }

        return request.OrganizationId.HasValue &&
               userContext.AllowedOrganizationIds.Contains(request.OrganizationId.Value);
    }

    private static bool IsKioskScopeAllowed(
        CurrentUserContext userContext,
        AccountRoleScopeRequest request)
    {
        if (request.KioskId.HasValue && userContext.AllowedKioskIds.Contains(request.KioskId.Value))
        {
            return true;
        }

        if (request.StoreId.HasValue && userContext.AllowedStoreIds.Contains(request.StoreId.Value))
        {
            return true;
        }

        return request.OrganizationId.HasValue &&
               userContext.AllowedOrganizationIds.Contains(request.OrganizationId.Value);
    }
}
