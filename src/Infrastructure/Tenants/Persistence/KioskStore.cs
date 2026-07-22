using Application.Tenants.Abstractions;
using Domain.Common.Enums;
using Domain.Tenants.Entities;
using Domain.Tenants.Enums;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Application.Tenants.Kiosks.Rules;
using Domain.ProductionExecution.Enums;
using Domain.Sync.Enums;

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
        return _dbContext.Stores.WhereNotDeleted().FirstOrDefaultAsync(x => x.Id == storeId, cancellationToken);
    }

    public Task<bool> OrganizationExistsActiveAsync(Guid organizationId, CancellationToken cancellationToken = default)
    {
        return _dbContext.Organizations.WhereNotDeleted()
            .AnyAsync(x => x.Id == organizationId && x.Status == EntityStatus.Active, cancellationToken);
    }

    public Task<bool> StoreExistsActiveAsync(Guid storeId, CancellationToken cancellationToken = default)
    {
        return _dbContext.Stores.WhereNotDeleted()
            .AnyAsync(x => x.Id == storeId && x.Status == EntityStatus.Active, cancellationToken);
    }

    public Task<bool> KioskCodeExistsAsync(Guid organizationId, string code, Guid? excludeKioskId = null, CancellationToken cancellationToken = default)
    {
        var normalized = code.Trim().ToUpperInvariant();
        var query = _dbContext.Kiosks.WhereNotDeleted().Where(x => x.OrganizationId == organizationId && x.Code.ToUpper() == normalized);
        if (excludeKioskId.HasValue)
        {
            query = query.Where(x => x.Id != excludeKioskId.Value);
        }
        return query.AnyAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Kiosk>> ListAsync(Guid? organizationId, Guid? storeId, KioskStatus? status, string? search, CancellationToken cancellationToken = default)
    {
        var query = _dbContext.Kiosks.WhereNotDeleted();
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
        var query = _dbContext.Kiosks.WhereNotDeleted().Where(x =>
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
        return _dbContext.Kiosks.WhereNotDeleted().FirstOrDefaultAsync(x => x.Id == kioskId, cancellationToken);
    }

    public Task<Kiosk?> GetByStoreAndIdAsync(
        Guid storeId,
        Guid kioskId,
        CancellationToken cancellationToken = default) =>
        _dbContext.Kiosks.WhereNotDeleted()
            .FirstOrDefaultAsync(x => x.Id == kioskId && x.StoreId == storeId, cancellationToken);

    public Task<bool> HasRunningExecutionAsync(
        Guid kioskId,
        CancellationToken cancellationToken = default) =>
        _dbContext.EdgeCommands.AnyAsync(command =>
            command.KioskId == kioskId &&
            command.CommandType == EdgeCommandType.ExecuteOrder &&
            command.Status == EdgeCommandStatus.Accepted &&
            !_dbContext.OrderExecutionRecords.Any(record =>
                record.SourceCommandId == command.Id &&
                (record.Status == ProductionExecutionStatus.Completed ||
                 record.Status == ProductionExecutionStatus.Failed ||
                 record.Status == ProductionExecutionStatus.RequiresManualIntervention)),
            cancellationToken);

    public Task AddOperationalStateTransitionAsync(
        KioskOperationalStateTransition transition,
        CancellationToken cancellationToken = default) =>
        _dbContext.KioskOperationalStateTransitions.AddAsync(transition, cancellationToken).AsTask();

    public async Task<T> ExecuteOperationalStateSerializedAsync<T>(
        Guid kioskId,
        Func<CancellationToken, Task<T>> action,
        CancellationToken cancellationToken = default)
    {
        await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
        await _dbContext.Database.ExecuteSqlInterpolatedAsync(
            $"SELECT pg_advisory_xact_lock(hashtextextended({KioskOperationalConcurrency.LockKey(kioskId)}, 0));",
            cancellationToken);
        var result = await action(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return result;
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
