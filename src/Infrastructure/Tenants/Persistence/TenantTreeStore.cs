using Application.Tenants.Abstractions;
using Domain.Common.Enums;
using Domain.Tenants.Entities;
using Domain.Tenants.Enums;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Tenants.Persistence;

public sealed class TenantTreeStore : ITenantTreeStore
{
    private readonly IceBotDbContext _dbContext;

    public TenantTreeStore(IceBotDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<Organization>> ListOrganizationsAsync(
        bool includeInactive,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.Organizations.WhereNotDeleted().AsNoTracking();
        if (!includeInactive)
        {
            query = query.Where(organization => organization.Status == EntityStatus.Active);
        }

        return await query.OrderBy(organization => organization.Code).ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Store>> ListStoresAsync(
        bool includeInactive,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.Stores.WhereNotDeleted().AsNoTracking();
        if (!includeInactive)
        {
            query = query.Where(store => store.Status == EntityStatus.Active);
        }

        return await query.OrderBy(store => store.Code).ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Kiosk>> ListKiosksAsync(
        bool includeInactive,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.Kiosks.WhereNotDeleted().AsNoTracking();
        if (!includeInactive)
        {
            query = query.Where(kiosk =>
                kiosk.Status != KioskStatus.Disabled &&
                kiosk.Status != KioskStatus.Retired);
        }

        return await query.OrderBy(kiosk => kiosk.Code).ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Store>> ListStoresByIdsAsync(
        IEnumerable<Guid> storeIds,
        bool includeInactive,
        CancellationToken cancellationToken = default)
    {
        var ids = storeIds.Distinct().ToArray();
        if (ids.Length == 0)
        {
            return Array.Empty<Store>();
        }

        var query = _dbContext.Stores.WhereNotDeleted().AsNoTracking().Where(store => ids.Contains(store.Id));
        if (!includeInactive)
        {
            query = query.Where(store => store.Status == EntityStatus.Active);
        }

        return await query.OrderBy(store => store.Code).ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Kiosk>> ListKiosksByIdsAsync(
        IEnumerable<Guid> kioskIds,
        bool includeInactive,
        CancellationToken cancellationToken = default)
    {
        var ids = kioskIds.Distinct().ToArray();
        if (ids.Length == 0)
        {
            return Array.Empty<Kiosk>();
        }

        var query = _dbContext.Kiosks.WhereNotDeleted().AsNoTracking().Where(kiosk => ids.Contains(kiosk.Id));
        if (!includeInactive)
        {
            query = query.Where(kiosk =>
                kiosk.Status != KioskStatus.Disabled &&
                kiosk.Status != KioskStatus.Retired);
        }

        return await query.OrderBy(kiosk => kiosk.Code).ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Organization>> ListOrganizationsByIdsAsync(
        IEnumerable<Guid> organizationIds,
        bool includeInactive,
        CancellationToken cancellationToken = default)
    {
        var ids = organizationIds.Distinct().ToArray();
        if (ids.Length == 0)
        {
            return Array.Empty<Organization>();
        }

        var query = _dbContext.Organizations.WhereNotDeleted().AsNoTracking()
            .Where(organization => ids.Contains(organization.Id));
        if (!includeInactive)
        {
            query = query.Where(organization => organization.Status == EntityStatus.Active);
        }

        return await query.OrderBy(organization => organization.Code).ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Store>> ListStoresForScopeAsync(
        IEnumerable<Guid> organizationIds,
        IEnumerable<Guid> storeIds,
        bool includeInactive,
        CancellationToken cancellationToken = default)
    {
        var organizationIdArray = organizationIds.Distinct().ToArray();
        var storeIdArray = storeIds.Distinct().ToArray();
        if (organizationIdArray.Length == 0 && storeIdArray.Length == 0)
        {
            return Array.Empty<Store>();
        }

        var query = _dbContext.Stores.WhereNotDeleted().AsNoTracking()
            .Where(store => organizationIdArray.Contains(store.OrganizationId) || storeIdArray.Contains(store.Id));
        if (!includeInactive)
        {
            query = query.Where(store => store.Status == EntityStatus.Active);
        }

        return await query.OrderBy(store => store.Code).ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Kiosk>> ListKiosksForScopeAsync(
        IEnumerable<Guid> organizationIds,
        IEnumerable<Guid> storeIds,
        IEnumerable<Guid> kioskIds,
        bool includeInactive,
        CancellationToken cancellationToken = default)
    {
        var organizationIdArray = organizationIds.Distinct().ToArray();
        var storeIdArray = storeIds.Distinct().ToArray();
        var kioskIdArray = kioskIds.Distinct().ToArray();
        if (organizationIdArray.Length == 0 && storeIdArray.Length == 0 && kioskIdArray.Length == 0)
        {
            return Array.Empty<Kiosk>();
        }

        var query = _dbContext.Kiosks.WhereNotDeleted().AsNoTracking()
            .Where(kiosk =>
                organizationIdArray.Contains(kiosk.OrganizationId) ||
                storeIdArray.Contains(kiosk.StoreId) ||
                kioskIdArray.Contains(kiosk.Id));
        if (!includeInactive)
        {
            query = query.Where(kiosk =>
                kiosk.Status != KioskStatus.Disabled &&
                kiosk.Status != KioskStatus.Retired);
        }

        return await query.OrderBy(kiosk => kiosk.Code).ToListAsync(cancellationToken);
    }
}
