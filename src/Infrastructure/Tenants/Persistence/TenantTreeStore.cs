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
        var query = _dbContext.Organizations.AsNoTracking();
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
        var query = _dbContext.Stores.AsNoTracking();
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
        var query = _dbContext.Kiosks.AsNoTracking();
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

        var query = _dbContext.Stores.AsNoTracking().Where(store => ids.Contains(store.Id));
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

        var query = _dbContext.Kiosks.AsNoTracking().Where(kiosk => ids.Contains(kiosk.Id));
        if (!includeInactive)
        {
            query = query.Where(kiosk =>
                kiosk.Status != KioskStatus.Disabled &&
                kiosk.Status != KioskStatus.Retired);
        }

        return await query.OrderBy(kiosk => kiosk.Code).ToListAsync(cancellationToken);
    }
}
