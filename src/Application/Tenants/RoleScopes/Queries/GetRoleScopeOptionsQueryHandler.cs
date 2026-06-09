using Application.Identity.Roles.Rules;
using Application.Identity.Tokens.Claims;
using Application.Shared.Wrappers;
using Application.Tenants.Abstractions;
using Application.Tenants.RoleScopes.Mapping;
using Application.Tenants.RoleScopes.Results;
using Application.Tenants.RoleScopes.Rules;
using Domain.Tenants.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

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
                userContext.AllowedStoreIds,
                includeInactive: false,
                cancellationToken);

            var scopedKiosks = await _tenantTreeStore.ListKiosksByIdsAsync(
                userContext.AllowedKioskIds,
                includeInactive: false,
                cancellationToken);

            organizations = await _tenantTreeStore.ListOrganizationsAsync(includeInactive: false, cancellationToken);
            stores = await _tenantTreeStore.ListStoresAsync(includeInactive: false, cancellationToken);
            kiosks = await _tenantTreeStore.ListKiosksAsync(includeInactive: false, cancellationToken);

            var allowedOrganizationIds = userContext.AllowedOrganizationIds
                .Concat(scopedStores.Select(store => store.OrganizationId))
                .Concat(scopedKiosks.Select(kiosk => kiosk.OrganizationId))
                .ToHashSet();

            var allowedStoreIds = userContext.AllowedStoreIds
                .Concat(scopedKiosks.Select(kiosk => kiosk.StoreId))
                .ToHashSet();

            var allowedKioskIds = userContext.AllowedKioskIds.ToHashSet();

            organizations = organizations
                .Where(organization => allowedOrganizationIds.Contains(organization.Id))
                .ToList();

            stores = stores
                .Where(store =>
                    userContext.AllowedOrganizationIds.Contains(store.OrganizationId) ||
                    allowedStoreIds.Contains(store.Id))
                .ToList();

            kiosks = kiosks
                .Where(kiosk =>
                    userContext.AllowedOrganizationIds.Contains(kiosk.OrganizationId) ||
                    userContext.AllowedStoreIds.Contains(kiosk.StoreId) ||
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
