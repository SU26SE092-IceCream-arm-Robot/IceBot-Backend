using Application.Orders.IncidentResolution.Abstractions;
using Domain.Orders.Entities;
using Domain.Orders.Incidents;
using Domain.ProductionExecution.Projections;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Orders.Persistence;

public sealed class ProductionIncidentStore(IceBotDbContext db) : IProductionIncidentStore
{
    public async Task<T> ExecuteInTransactionAsync<T>(Func<CancellationToken, Task<T>> action, CancellationToken ct = default)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        try
        {
            var result = await action(ct);
            await transaction.CommitAsync(ct);
            return result;
        }
        catch
        {
            await transaction.RollbackAsync(ct);
            throw;
        }
    }

    public Task AcquireIncidentLockAsync(Guid incidentId, CancellationToken ct = default) =>
        db.Database.ExecuteSqlInterpolatedAsync(
            $"SELECT pg_advisory_xact_lock(hashtextextended({$"production-incident:{incidentId:D}"}, 0));", ct);

    public Task AcquireSourceLockAsync(Guid commandId, Guid jobId, CancellationToken ct = default) =>
        db.Database.ExecuteSqlInterpolatedAsync(
            $"SELECT pg_advisory_xact_lock(hashtextextended({$"production-incident-source:{commandId:D}:{jobId:D}"}, 0));", ct);

    public Task<ProductionIncident?> GetByIdAsync(Guid id, bool tracked, CancellationToken ct = default)
    {
        var query = db.ProductionIncidents.Include(x => x.History).AsQueryable();
        if (!tracked) query = query.AsNoTracking();
        return query.FirstOrDefaultAsync(x => x.Id == id, ct);
    }

    public Task<ProductionIncident?> GetBySourceAsync(Guid commandId, Guid jobId, CancellationToken ct = default) =>
        db.ProductionIncidents.Include(x => x.History).FirstOrDefaultAsync(x =>
            x.SourceCommandId == commandId && x.SourceProductionJobId == jobId, ct);

    public async Task<(IReadOnlyList<ProductionIncident> Items, int Total)> ListAsync(
        ProductionIncidentStatus? status, Guid? organizationId, Guid? storeId, Guid? kioskId,
        bool isSystemAdmin, IReadOnlyCollection<Guid> allowedOrganizationIds,
        IReadOnlyCollection<Guid> allowedStoreIds, IReadOnlyCollection<Guid> allowedKioskIds,
        int pageNumber, int pageSize, CancellationToken ct = default)
    {
        var query = db.ProductionIncidents.AsNoTracking().AsQueryable();
        if (status.HasValue) query = query.Where(x => x.Status == status.Value);
        if (organizationId.HasValue) query = query.Where(x => x.OrganizationId == organizationId);
        if (storeId.HasValue) query = query.Where(x => x.StoreId == storeId);
        if (kioskId.HasValue) query = query.Where(x => x.KioskId == kioskId);
        if (!isSystemAdmin)
            query = query.Where(x =>
                (x.OrganizationId.HasValue && allowedOrganizationIds.Contains(x.OrganizationId.Value)) ||
                (x.StoreId.HasValue && allowedStoreIds.Contains(x.StoreId.Value)) ||
                allowedKioskIds.Contains(x.KioskId));
        var total = await query.CountAsync(ct);
        var items = await query.OrderBy(x => x.Status).ThenByDescending(x => x.CreatedAt)
            .Skip((pageNumber - 1) * pageSize).Take(pageSize).ToListAsync(ct);
        return (items, total);
    }

    public Task<Order?> GetOrderAsync(Guid orderId, CancellationToken ct = default) =>
        db.Orders.Include(x => x.OrderItems).FirstOrDefaultAsync(x => x.Id == orderId, ct);

    public Task<ProductionExecutionRecord?> GetProductionRecordAsync(Guid commandId, Guid jobId, CancellationToken ct = default) =>
        db.ProductionExecutionRecords.AsNoTracking().FirstOrDefaultAsync(x =>
            x.SourceCommandId == commandId && x.SourceProductionJobId == jobId, ct);

    public Task AddAsync(ProductionIncident incident, CancellationToken ct = default) =>
        db.ProductionIncidents.AddAsync(incident, ct).AsTask();

    public Task<int> SaveChangesAsync(CancellationToken ct = default) => db.SaveChangesAsync(ct);
}
