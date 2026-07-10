using Application.Operations.OperationLogs.Abstractions;
using Domain.Common.Enums;
using Domain.Operations.Entities;
using Domain.Tenants.Entities;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Operations.Persistence;

public sealed class OperationLogStore : IOperationLogStore
{
    private readonly IceBotDbContext _dbContext;

    public OperationLogStore(IceBotDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<Kiosk?> GetKioskByIdAsync(Guid kioskId, CancellationToken cancellationToken = default) =>
        _dbContext.Kiosks.WhereNotDeleted()
            .AsNoTracking()
            .FirstOrDefaultAsync(kiosk => kiosk.Id == kioskId, cancellationToken);

    public Task<int> CountAsync(
        Guid kioskId,
        Guid? deviceId,
        Guid? orderId,
        SeverityLevel? severity,
        DateTimeOffset? from,
        DateTimeOffset? to,
        CancellationToken cancellationToken = default) =>
        ApplyFilters(kioskId, deviceId, orderId, severity, from, to)
            .CountAsync(cancellationToken);

    public Task<List<OperationLog>> ListAsync(
        Guid kioskId,
        Guid? deviceId,
        Guid? orderId,
        SeverityLevel? severity,
        DateTimeOffset? from,
        DateTimeOffset? to,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default) =>
        ApplyFilters(kioskId, deviceId, orderId, severity, from, to)
            .OrderByDescending(log => log.OccurredAt)
            .ThenByDescending(log => log.Id)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

    public Task<OperationLog?> GetByKioskIdAsync(
        Guid kioskId,
        Guid operationLogId,
        CancellationToken cancellationToken = default) =>
        _dbContext.OperationLogs
            .AsNoTracking()
            .FirstOrDefaultAsync(
                log => log.Id == operationLogId && log.KioskId == kioskId,
                cancellationToken);

    private IQueryable<OperationLog> ApplyFilters(
        Guid kioskId,
        Guid? deviceId,
        Guid? orderId,
        SeverityLevel? severity,
        DateTimeOffset? from,
        DateTimeOffset? to)
    {
        var query = _dbContext.OperationLogs
            .AsNoTracking()
            .Where(log => log.KioskId == kioskId);

        if (deviceId.HasValue) query = query.Where(log => log.DeviceId == deviceId.Value);
        if (orderId.HasValue) query = query.Where(log => log.OrderId == orderId.Value);
        if (severity.HasValue) query = query.Where(log => log.Severity == severity.Value);
        if (from.HasValue) query = query.Where(log => log.OccurredAt >= from.Value);
        if (to.HasValue) query = query.Where(log => log.OccurredAt <= to.Value);

        return query;
    }
}
