using Application.Shared.Wrappers;
using Application.Tenants.Abstractions;
using Application.Tenants.TenantTree.Results;
using Domain.Tenants.Entities;

namespace Application.Tenants.TenantTree.Queries;

public sealed class GetTenantTreeQueryHandler
{
    private readonly ITenantTreeStore _tenantTreeStore;

    public GetTenantTreeQueryHandler(ITenantTreeStore tenantTreeStore)
    {
        _tenantTreeStore = tenantTreeStore;
    }

    public async Task<ApiResult<TenantTreeResult>> HandleAsync(
        GetTenantTreeQuery query,
        CancellationToken cancellationToken = default)
    {
        var userContext = query.UserContext;
        var includeInactive = query.IncludeInactive;

        IReadOnlyList<Organization> organizations;
        IReadOnlyList<Store> stores;
        IReadOnlyList<Kiosk> kiosks;

        if (userContext.IsSystemAdmin)
        {
            organizations = await _tenantTreeStore.ListOrganizationsAsync(includeInactive, cancellationToken);
            stores = await _tenantTreeStore.ListStoresAsync(includeInactive, cancellationToken);
            kiosks = await _tenantTreeStore.ListKiosksAsync(includeInactive, cancellationToken);
        }
        else
        {
            var scopedStores = await _tenantTreeStore.ListStoresByIdsAsync(
                userContext.AllowedStoreIds,
                includeInactive,
                cancellationToken);

            var scopedKiosks = await _tenantTreeStore.ListKiosksByIdsAsync(
                userContext.AllowedKioskIds,
                includeInactive,
                cancellationToken);

            organizations = await _tenantTreeStore.ListOrganizationsAsync(includeInactive, cancellationToken);
            stores = await _tenantTreeStore.ListStoresAsync(includeInactive, cancellationToken);
            kiosks = await _tenantTreeStore.ListKiosksAsync(includeInactive, cancellationToken);

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

        return ApiResult<TenantTreeResult>.Success(BuildTree(organizations, stores, kiosks));
    }

    private static TenantTreeResult BuildTree(
        IReadOnlyList<Organization> organizations,
        IReadOnlyList<Store> stores,
        IReadOnlyList<Kiosk> kiosks)
    {
        var kiosksByStore = kiosks
            .GroupBy(kiosk => kiosk.StoreId)
            .ToDictionary(group => group.Key, group => group.OrderBy(kiosk => kiosk.Code).ToList());

        var storesByOrganization = stores
            .GroupBy(store => store.OrganizationId)
            .ToDictionary(group => group.Key, group => group.OrderBy(store => store.Code).ToList());

        var organizationResults = organizations
            .OrderBy(organization => organization.Code)
            .Select(organization => new TenantTreeOrganizationResult
            {
                Id = organization.Id,
                Code = organization.Code,
                Name = organization.Name,
                Status = organization.Status.ToString(),
                Stores = storesByOrganization.TryGetValue(organization.Id, out var organizationStores)
                    ? organizationStores.Select(store => ToStoreResult(store, kiosksByStore)).ToList()
                    : Array.Empty<TenantTreeStoreResult>()
            })
            .ToList();

        return new TenantTreeResult
        {
            Organizations = organizationResults
        };
    }

    private static TenantTreeStoreResult ToStoreResult(
        Store store,
        IReadOnlyDictionary<Guid, List<Kiosk>> kiosksByStore)
    {
        return new TenantTreeStoreResult
        {
            Id = store.Id,
            OrganizationId = store.OrganizationId,
            Code = store.Code,
            Name = store.Name,
            Status = store.Status.ToString(),
            Kiosks = kiosksByStore.TryGetValue(store.Id, out var storeKiosks)
                ? storeKiosks.Select(ToKioskResult).ToList()
                : Array.Empty<TenantTreeKioskResult>()
        };
    }

    private static TenantTreeKioskResult ToKioskResult(Kiosk kiosk)
    {
        return new TenantTreeKioskResult
        {
            Id = kiosk.Id,
            OrganizationId = kiosk.OrganizationId,
            StoreId = kiosk.StoreId,
            Code = kiosk.Code,
            Name = kiosk.Name,
            Status = kiosk.Status.ToString()
        };
    }
}
