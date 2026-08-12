using Application.Tenants.Abstractions;
using Domain.Common.Enums;
using Domain.Tenants.Enums;
using Domain.Tenants.Entities;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System.Data;

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
        var query = _dbContext.Organizations.WhereNotDeleted();
        if (asNoTracking)
        {
            query = query.AsNoTracking();
        }
        return query.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public Task<Organization?> GetByCodeAsync(string code, bool asNoTracking = true, CancellationToken cancellationToken = default)
    {
        var query = _dbContext.Organizations.WhereNotDeleted();
        if (asNoTracking)
        {
            query = query.AsNoTracking();
        }
        return query.FirstOrDefaultAsync(x => x.Code == code, cancellationToken);
    }

    public Task<List<Organization>> ListAsync(string? search, string? status, int pageNumber, int pageSize, CancellationToken cancellationToken = default)
    {
        var query = ApplyFilters(_dbContext.Organizations.WhereNotDeleted(), search, status);
        return query.AsNoTracking()
            .OrderBy(x => x.Code)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
    }

    public Task<List<Organization>> ListByIdsAsync(IEnumerable<Guid> ids, string? search, string? status, int pageNumber, int pageSize, CancellationToken cancellationToken = default)
    {
        var query = _dbContext.Organizations.WhereNotDeleted().Where(x => ids.Contains(x.Id));
        query = ApplyFilters(query, search, status);
        return query.AsNoTracking()
            .OrderBy(x => x.Code)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
    }

    public Task<int> CountAsync(string? search, string? status, CancellationToken cancellationToken = default)
    {
        return ApplyFilters(_dbContext.Organizations.WhereNotDeleted().AsNoTracking(), search, status).CountAsync(cancellationToken);
    }

    public Task<int> CountByIdsAsync(IEnumerable<Guid> ids, string? search, string? status, CancellationToken cancellationToken = default)
    {
        return ApplyFilters(_dbContext.Organizations.WhereNotDeleted().Where(x => ids.Contains(x.Id)).AsNoTracking(), search, status).CountAsync(cancellationToken);
    }

    public Task<bool> ExistsByCodeAsync(string code, CancellationToken cancellationToken = default)
    {
        return _dbContext.Organizations.WhereNotDeleted().AnyAsync(x => x.Code == code, cancellationToken);
    }

    public Task<OrganizationStatusTransition?> GetStatusTransitionByIdempotencyKeyAsync(
        Guid organizationId,
        string idempotencyKey,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.OrganizationStatusTransitions
            .AsNoTracking()
            .FirstOrDefaultAsync(
                transition => transition.OrganizationId == organizationId &&
                              transition.RequestIdempotencyKey == idempotencyKey,
                cancellationToken);
    }

    public async Task<IReadOnlyList<OrganizationStatusTransition>> ListStatusTransitionsAsync(
        Guid organizationId,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.OrganizationStatusTransitions
            .AsNoTracking()
            .Where(transition => transition.OrganizationId == organizationId)
            .OrderByDescending(transition => transition.ChangedAt)
            .ThenByDescending(transition => transition.Id)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<OrganizationStatusTransition>> ListDueSessionRevocationTransitionsAsync(
        DateTimeOffset now,
        int batchSize,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.OrganizationStatusTransitions
            .AsNoTracking()
            .Where(transition =>
                transition.SessionRevocationStatus != OrganizationLifecycleSideEffectStatus.Completed &&
                transition.NextSessionRevocationAttemptAt != null &&
                transition.NextSessionRevocationAttemptAt <= now)
            .OrderBy(transition => transition.NextSessionRevocationAttemptAt)
            .ThenBy(transition => transition.Id)
            .Take(batchSize)
            .ToListAsync(cancellationToken);
    }

    public Task<OrganizationStatusTransition?> GetStatusTransitionByIdAsync(
        Guid transitionId,
        bool asNoTracking = true,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.OrganizationStatusTransitions.AsQueryable();
        if (asNoTracking)
        {
            query = query.AsNoTracking();
        }

        return query.FirstOrDefaultAsync(transition => transition.Id == transitionId, cancellationToken);
    }

    public async Task<IReadOnlyList<Guid>> ListAccountIdsWithOrganizationScopeAsync(
        Guid organizationId,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.AccountRoles
            .AsNoTracking()
            .Where(role => role.IsActive &&
                (role.OrganizationId == organizationId ||
                 (role.StoreId != null && role.Store!.OrganizationId == organizationId) ||
                 (role.KioskId != null && role.Kiosk!.OrganizationId == organizationId)))
            .Select(role => role.AccountId)
            .Distinct()
            .ToListAsync(cancellationToken);
    }

    public Task AddAsync(Organization organization, CancellationToken cancellationToken = default)
    {
        return _dbContext.Organizations.AddAsync(organization, cancellationToken).AsTask();
    }

    public Task AddStatusTransitionAsync(OrganizationStatusTransition transition, CancellationToken cancellationToken = default)
    {
        return _dbContext.OrganizationStatusTransitions.AddAsync(transition, cancellationToken).AsTask();
    }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<T> ExecuteInTransactionAsync<T>(Func<Task<T>> operation, CancellationToken cancellationToken = default)
    {
        if (_dbContext.Database.CurrentTransaction is not null)
        {
            return await operation();
        }

        var strategy = _dbContext.Database.CreateExecutionStrategy();
        return await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await _dbContext.Database.BeginTransactionAsync(
                IsolationLevel.Serializable,
                cancellationToken);
            var result = await operation();
            await transaction.CommitAsync(cancellationToken);
            return result;
        });
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
