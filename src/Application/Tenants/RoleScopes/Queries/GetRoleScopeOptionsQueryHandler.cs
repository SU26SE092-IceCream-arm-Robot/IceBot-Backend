using Application.Identity.Roles.Rules;
using Application.Identity.Tokens.Claims;
using Application.Shared.Wrappers;
using Application.Tenants.Abstractions;
using Application.Tenants.RoleScopes.Mapping;
using Application.Tenants.RoleScopes.Results;
using Application.Tenants.RoleScopes.Rules;
using Domain.Tenants.Entities;

namespace Application.Tenants.RoleScopes.Queries;

public sealed class GetRoleScopeOptionsQueryHandler
{
    private readonly ITenantTreeStore _tenantTreeStore;

    public GetRoleScopeOptionsQueryHandler(ITenantTreeStore tenantTreeStore)
    {
        _tenantTreeStore = tenantTreeStore;
    }

    public async Task<ApiResult<RoleScopeOptionsResult>> HandleAsync(
        GetRoleScopeOptionsQuery query,
        CancellationToken cancellationToken = default)
    {
        var roleCode = query.RoleCode?.Trim();
        if (string.IsNullOrWhiteSpace(roleCode) || !RoleScopeRules.ScopeMetadata.TryGetValue(roleCode, out var meta))
        {
            return ApiResult<RoleScopeOptionsResult>.Fail("Invalid or unsupported role code.", 400);
        }

        var userContext = query.UserContext;
        var scope = ScopeAccessRules.GetEffectiveScope(ScopeRoleSets.RoleScopeOptionsView, userContext);
        if (!CanRequestRoleScopeOptions(userContext, query.UserRoles, roleCode))
        {
            return ApiResult<RoleScopeOptionsResult>.Fail(
                "Current account is not allowed to assign this role.",
                403);
        }

        IReadOnlyList<Organization> organizations;
        IReadOnlyList<Store> stores;
        IReadOnlyList<Kiosk> kiosks;

        if (userContext.IsSystemAdmin)
        {
            organizations = await _tenantTreeStore.ListOrganizationsAsync(includeInactive: false, cancellationToken);
            stores = await _tenantTreeStore.ListStoresAsync(includeInactive: false, cancellationToken);
            kiosks = await _tenantTreeStore.ListKiosksAsync(includeInactive: false, cancellationToken);
        }
        else
        {
            var scopedStores = await _tenantTreeStore.ListStoresByIdsAsync(
                scope.StoreIds,
                includeInactive: false,
                cancellationToken);

            var scopedKiosks = await _tenantTreeStore.ListKiosksByIdsAsync(
                scope.KioskIds,
                includeInactive: false,
                cancellationToken);

            organizations = await _tenantTreeStore.ListOrganizationsAsync(includeInactive: false, cancellationToken);
            stores = await _tenantTreeStore.ListStoresAsync(includeInactive: false, cancellationToken);
            kiosks = await _tenantTreeStore.ListKiosksAsync(includeInactive: false, cancellationToken);

            var allowedOrganizationIds = scope.OrganizationIds
                .Concat(scopedStores.Select(store => store.OrganizationId))
                .Concat(scopedKiosks.Select(kiosk => kiosk.OrganizationId))
                .ToHashSet();

            var allowedStoreIds = scope.StoreIds
                .Concat(scopedKiosks.Select(kiosk => kiosk.StoreId))
                .ToHashSet();

            var allowedKioskIds = scope.KioskIds.ToHashSet();

            organizations = organizations
                .Where(organization => allowedOrganizationIds.Contains(organization.Id))
                .ToList();

            stores = stores
                .Where(store =>
                    scope.OrganizationIds.Contains(store.OrganizationId) ||
                    allowedStoreIds.Contains(store.Id))
                .ToList();

            kiosks = kiosks
                .Where(kiosk =>
                    scope.OrganizationIds.Contains(kiosk.OrganizationId) ||
                    scope.StoreIds.Contains(kiosk.StoreId) ||
                    allowedKioskIds.Contains(kiosk.Id))
                .ToList();
        }

        var result = RoleScopeOptionsResultMapper.ToResult(
            roleCode,
            meta.AllowedScopes,
            meta.RequiresScope,
            organizations,
            stores,
            kiosks);

        return ApiResult<RoleScopeOptionsResult>.Success(result, "Role scope options retrieved successfully.");
    }

    private static bool CanRequestRoleScopeOptions(
        CurrentUserContext userContext,
        IReadOnlyCollection<string> userRoles,
        string targetRoleCode)
    {
        if (userContext.IsSystemAdmin ||
            userRoles.Contains("SystemAdmin", StringComparer.OrdinalIgnoreCase))
        {
            return true;
        }

        return userRoles.Any(roleCode => RoleCatalogRules.CanAssignRole(roleCode, targetRoleCode));
    }
}
