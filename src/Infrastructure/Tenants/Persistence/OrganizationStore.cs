using Application.Tenants.Abstractions;
using Domain.Common.Enums;
using Domain.Tenants.Entities;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Tenants.Persistence;

public sealed class OrganizationStore : IOrganizationStore
{
    private readonly IceBotDbContext _dbContext;

    public OrganizationStore(IceBotDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<Organization?> GetByIdAsync(Guid id, bool asNoTracking = true, CancellationToken cancellationToken = default)
    {
        var query = _dbContext.Organizations.AsQueryable();
        if (asNoTracking)
        {
            query = query.AsNoTracking();
        }
        return query.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public Task<Organization?> GetByCodeAsync(string code, bool asNoTracking = true, CancellationToken cancellationToken = default)
    {
        var query = _dbContext.Organizations.AsQueryable();
        if (asNoTracking)
        {
            query = query.AsNoTracking();
        }
        return query.FirstOrDefaultAsync(x => x.Code == code, cancellationToken);
    }

    public Task<List<Organization>> ListAsync(string? search, string? status, int pageNumber, int pageSize, CancellationToken cancellationToken = default)
    {
        var query = ApplyFilters(_dbContext.Organizations.AsQueryable(), search, status);
        return query.AsNoTracking()
            .OrderBy(x => x.Code)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
    }

    public Task<List<Organization>> ListByIdsAsync(IEnumerable<Guid> ids, string? search, string? status, int pageNumber, int pageSize, CancellationToken cancellationToken = default)
    {
        var query = _dbContext.Organizations.Where(x => ids.Contains(x.Id));
        query = ApplyFilters(query, search, status);
        return query.AsNoTracking()
            .OrderBy(x => x.Code)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
    }

    public Task<int> CountAsync(string? search, string? status, CancellationToken cancellationToken = default)
    {
        return ApplyFilters(_dbContext.Organizations.AsNoTracking(), search, status).CountAsync(cancellationToken);
    }

    public Task<int> CountByIdsAsync(IEnumerable<Guid> ids, string? search, string? status, CancellationToken cancellationToken = default)
    {
        return ApplyFilters(_dbContext.Organizations.Where(x => ids.Contains(x.Id)).AsNoTracking(), search, status).CountAsync(cancellationToken);
    }

    public Task<bool> ExistsByCodeAsync(string code, CancellationToken cancellationToken = default)
    {
        return _dbContext.Organizations.AnyAsync(x => x.Code == code, cancellationToken);
    }

    public Task AddAsync(Organization organization, CancellationToken cancellationToken = default)
    {
        return _dbContext.Organizations.AddAsync(organization, cancellationToken).AsTask();
    }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return _dbContext.SaveChangesAsync(cancellationToken);
    }

    private static IQueryable<Organization> ApplyFilters(IQueryable<Organization> query, string? search, string? status)
    {
        if (!string.IsNullOrWhiteSpace(search))
        {
            var normalized = search.Trim().ToLowerInvariant();
            query = query.Where(x =>
                x.Code.ToLower().Contains(normalized) ||
                x.Name.ToLower().Contains(normalized) ||
                (x.LegalName != null && x.LegalName.ToLower().Contains(normalized)));
        }

        if (!string.IsNullOrWhiteSpace(status) &&
            Enum.TryParse<EntityStatus>(status.Trim(), ignoreCase: true, out var parsedStatus))
        {
            query = query.Where(x => x.Status == parsedStatus);
        }

        return query;
    }
}
