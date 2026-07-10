using Application.Tenants.Abstractions;
using Domain.Common.Enums;
using Domain.Tenants.Entities;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Tenants.Persistence;

public sealed class StoreStore : IStoreStore
{
    private readonly IceBotDbContext _dbContext;

    public StoreStore(IceBotDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<bool> OrganizationExistsActiveAsync(Guid organizationId, CancellationToken cancellationToken = default)
    {
        return _dbContext.Organizations.WhereNotDeleted()
            .AnyAsync(x => x.Id == organizationId && x.Status == EntityStatus.Active, cancellationToken);
    }

    public Task<bool> StoreCodeExistsAsync(Guid organizationId, string code, Guid? excludeStoreId = null, CancellationToken cancellationToken = default)
    {
        var normalizedCode = code.Trim().ToUpperInvariant();
        var query = _dbContext.Stores.WhereNotDeleted().Where(x => x.OrganizationId == organizationId && x.Code.ToUpper() == normalizedCode);
        if (excludeStoreId.HasValue)
        {
            query = query.Where(x => x.Id != excludeStoreId.Value);
        }
        return query.AnyAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Store>> ListAsync(Guid? organizationId, EntityStatus? status, string? search, CancellationToken cancellationToken = default)
    {
        var query = _dbContext.Stores.WhereNotDeleted();
        if (organizationId.HasValue)
        {
            query = query.Where(x => x.OrganizationId == organizationId.Value);
        }
        query = ApplyFilters(query, search, status);
        return await query.AsNoTracking().OrderBy(x => x.Code).ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Store>> ListByOrganizationIdsAsync(IEnumerable<Guid> organizationIds, EntityStatus? status, string? search, CancellationToken cancellationToken = default)
    {
        var query = _dbContext.Stores.WhereNotDeleted().Where(x => organizationIds.Contains(x.OrganizationId));
        query = ApplyFilters(query, search, status);
        return await query.AsNoTracking().OrderBy(x => x.Code).ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Store>> ListAccessibleAsync(
        IEnumerable<Guid> organizationIds,
        IEnumerable<Guid> storeIds,
        Guid? organizationId,
        EntityStatus? status,
        string? search,
        CancellationToken cancellationToken = default)
    {
        var scopedOrganizationIds = organizationIds.Distinct().ToArray();
        var scopedStoreIds = storeIds.Distinct().ToArray();

        if (scopedOrganizationIds.Length == 0 && scopedStoreIds.Length == 0)
        {
            return Array.Empty<Store>();
        }

        var query = _dbContext.Stores.WhereNotDeleted().Where(x =>
            scopedOrganizationIds.Contains(x.OrganizationId) ||
            scopedStoreIds.Contains(x.Id));

        if (organizationId.HasValue)
        {
            query = query.Where(x => x.OrganizationId == organizationId.Value);
        }

        query = ApplyFilters(query, search, status);
        return await query.AsNoTracking().OrderBy(x => x.Code).ToListAsync(cancellationToken);
    }

    public Task<Store?> GetByIdAsync(Guid storeId, CancellationToken cancellationToken = default)
    {
        return _dbContext.Stores.WhereNotDeleted().FirstOrDefaultAsync(x => x.Id == storeId, cancellationToken);
    }

    public Task AddAsync(Store store, CancellationToken cancellationToken = default)
    {
        return _dbContext.Stores.AddAsync(store, cancellationToken).AsTask();
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return _dbContext.SaveChangesAsync(cancellationToken);
    }

    private static IQueryable<Store> ApplyFilters(IQueryable<Store> query, string? search, EntityStatus? status)
    {
        if (!string.IsNullOrWhiteSpace(search))
        {
            var normalized = search.Trim().ToLowerInvariant();
            query = query.Where(x =>
                x.Code.ToLower().Contains(normalized) ||
                x.Name.ToLower().Contains(normalized) ||
                (x.Email != null && x.Email.ToLower().Contains(normalized)) ||
                (x.PhoneNumber != null && x.PhoneNumber.Contains(normalized)));
        }

        if (status.HasValue)
        {
            query = query.Where(x => x.Status == status.Value);
        }

        return query;
    }
}
