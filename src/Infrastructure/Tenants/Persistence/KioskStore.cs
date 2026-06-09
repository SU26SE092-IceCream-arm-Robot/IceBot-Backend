using Application.Tenants.Abstractions;
using Domain.Common.Enums;
using Domain.Tenants.Entities;
using Domain.Tenants.Enums;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Tenants.Persistence;

public sealed class KioskStore : IKioskStore
{
    private readonly IceBotDbContext _dbContext;

    public KioskStore(IceBotDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<Store?> GetStoreByIdAsync(Guid storeId, CancellationToken cancellationToken = default)
    {
        return _dbContext.Stores.FirstOrDefaultAsync(x => x.Id == storeId, cancellationToken);
    }

    public Task<bool> OrganizationExistsActiveAsync(Guid organizationId, CancellationToken cancellationToken = default)
    {
        return _dbContext.Organizations
            .AnyAsync(x => x.Id == organizationId && x.Status == EntityStatus.Active, cancellationToken);
    }

    public Task<bool> StoreExistsActiveAsync(Guid storeId, CancellationToken cancellationToken = default)
    {
        return _dbContext.Stores
            .AnyAsync(x => x.Id == storeId && x.Status == EntityStatus.Active, cancellationToken);
    }

    public Task<bool> KioskCodeExistsAsync(Guid organizationId, string code, Guid? excludeKioskId = null, CancellationToken cancellationToken = default)
    {
        var normalized = code.Trim().ToUpperInvariant();
        var query = _dbContext.Kiosks.Where(x => x.OrganizationId == organizationId && x.Code.ToUpper() == normalized);
        if (excludeKioskId.HasValue)
        {
            query = query.Where(x => x.Id != excludeKioskId.Value);
        }
        return query.AnyAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Kiosk>> ListAsync(Guid? organizationId, Guid? storeId, KioskStatus? status, string? search, CancellationToken cancellationToken = default)
    {
        var query = _dbContext.Kiosks.AsQueryable();
        if (organizationId.HasValue)
        {
            query = query.Where(x => x.OrganizationId == organizationId.Value);
        }
        if (storeId.HasValue)
        {
            query = query.Where(x => x.StoreId == storeId.Value);
        }
        query = ApplyFilters(query, search, status);
        return await query.AsNoTracking().OrderBy(x => x.Code).ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Kiosk>> ListAccessibleAsync(
        IEnumerable<Guid> organizationIds,
        IEnumerable<Guid> storeIds,
        IEnumerable<Guid> kioskIds,
        Guid? organizationId,
        Guid? storeId,
        KioskStatus? status,
        string? search,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.Kiosks.Where(x =>
            organizationIds.Contains(x.OrganizationId) ||
            storeIds.Contains(x.StoreId) ||
            kioskIds.Contains(x.Id));

        if (organizationId.HasValue)
        {
            query = query.Where(x => x.OrganizationId == organizationId.Value);
        }
        if (storeId.HasValue)
        {
            query = query.Where(x => x.StoreId == storeId.Value);
        }
        query = ApplyFilters(query, search, status);
        return await query.AsNoTracking().OrderBy(x => x.Code).ToListAsync(cancellationToken);
    }

    public Task<Kiosk?> GetByIdAsync(Guid kioskId, CancellationToken cancellationToken = default)
    {
        return _dbContext.Kiosks.FirstOrDefaultAsync(x => x.Id == kioskId, cancellationToken);
    }

    public Task AddAsync(Kiosk kiosk, CancellationToken cancellationToken = default)
    {
        return _dbContext.Kiosks.AddAsync(kiosk, cancellationToken).AsTask();
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return _dbContext.SaveChangesAsync(cancellationToken);
    }

    private static IQueryable<Kiosk> ApplyFilters(IQueryable<Kiosk> query, string? search, KioskStatus? status)
    {
        if (!string.IsNullOrWhiteSpace(search))
        {
            var normalized = search.Trim().ToLowerInvariant();
            query = query.Where(x =>
                x.Code.ToLower().Contains(normalized) ||
                x.Name.ToLower().Contains(normalized) ||
                (x.SerialNumber != null && x.SerialNumber.ToLower().Contains(normalized)));
        }

        if (status.HasValue)
        {
            query = query.Where(x => x.Status == status.Value);
        }

        return query;
    }
}
