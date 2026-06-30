using Application.Operations.Abstractions;
using Domain.Common.Enums;
using Domain.Operations.Entities;
using Domain.Operations.Enums;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Operations.Persistence;

public sealed class AlertStore : IAlertStore
{
    private readonly IceBotDbContext _dbContext;

    public AlertStore(IceBotDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<T> ExecuteSerializedAsync<T>(
        Guid alertId,
        Func<CancellationToken, Task<T>> action,
        CancellationToken cancellationToken = default)
    {
        await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
        var lockKey = $"alert-lifecycle:{alertId:D}";
        await _dbContext.Database.ExecuteSqlInterpolatedAsync(
            $"SELECT pg_advisory_xact_lock(hashtextextended({lockKey}, 0));",
            cancellationToken);
        var result = await action(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return result;
    }

    public Task<int> CountAsync(
        AlertStatus? status,
        SeverityLevel? severity,
        Guid? organizationId,
        Guid? storeId,
        Guid? kioskId,
        Guid? deviceId,
        DateTimeOffset? from,
        DateTimeOffset? to,
        bool isSystemAdmin,
        IReadOnlyCollection<Guid> allowedOrganizationIds,
        IReadOnlyCollection<Guid> allowedStoreIds,
        IReadOnlyCollection<Guid> allowedKioskIds,
        CancellationToken cancellationToken = default) =>
        ApplyFilters(status, severity, organizationId, storeId, kioskId, deviceId, from, to,
                isSystemAdmin, allowedOrganizationIds, allowedStoreIds, allowedKioskIds)
            .CountAsync(cancellationToken);

    public Task<List<Alert>> ListAsync(
        AlertStatus? status,
        SeverityLevel? severity,
        Guid? organizationId,
        Guid? storeId,
        Guid? kioskId,
        Guid? deviceId,
        DateTimeOffset? from,
        DateTimeOffset? to,
        bool isSystemAdmin,
        IReadOnlyCollection<Guid> allowedOrganizationIds,
        IReadOnlyCollection<Guid> allowedStoreIds,
        IReadOnlyCollection<Guid> allowedKioskIds,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default) =>
        ApplyFilters(status, severity, organizationId, storeId, kioskId, deviceId, from, to,
                isSystemAdmin, allowedOrganizationIds, allowedStoreIds, allowedKioskIds)
            .OrderByDescending(alert => alert.RaisedAt)
            .ThenByDescending(alert => alert.Id)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

    public Task<Alert?> GetByIdAsync(Guid alertId, CancellationToken cancellationToken = default) =>
        _dbContext.Alerts
            .Include(alert => alert.Kiosk)
            .Include(alert => alert.Device)
            .Include(alert => alert.AcknowledgedByAccount)
            .FirstOrDefaultAsync(alert => alert.Id == alertId && alert.DeletedAt == null, cancellationToken);

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) =>
        _dbContext.SaveChangesAsync(cancellationToken);

    private IQueryable<Alert> ApplyFilters(
        AlertStatus? status,
        SeverityLevel? severity,
        Guid? organizationId,
        Guid? storeId,
        Guid? kioskId,
        Guid? deviceId,
        DateTimeOffset? from,
        DateTimeOffset? to,
        bool isSystemAdmin,
        IReadOnlyCollection<Guid> allowedOrganizationIds,
        IReadOnlyCollection<Guid> allowedStoreIds,
        IReadOnlyCollection<Guid> allowedKioskIds)
    {
        var query = _dbContext.Alerts.AsNoTracking()
            .Include(alert => alert.Kiosk)
            .Where(alert => alert.DeletedAt == null);

        if (status.HasValue) query = query.Where(alert => alert.Status == status.Value);
        if (severity.HasValue) query = query.Where(alert => alert.Severity == severity.Value);
        if (organizationId.HasValue) query = query.Where(alert => alert.Kiosk.OrganizationId == organizationId.Value);
        if (storeId.HasValue) query = query.Where(alert => alert.Kiosk.StoreId == storeId.Value);
        if (kioskId.HasValue) query = query.Where(alert => alert.KioskId == kioskId.Value);
        if (deviceId.HasValue) query = query.Where(alert => alert.DeviceId == deviceId.Value);
        if (from.HasValue) query = query.Where(alert => alert.RaisedAt >= from.Value);
        if (to.HasValue) query = query.Where(alert => alert.RaisedAt <= to.Value);

        if (!isSystemAdmin)
        {
            query = query.Where(alert =>
                allowedOrganizationIds.Contains(alert.Kiosk.OrganizationId) ||
                allowedStoreIds.Contains(alert.Kiosk.StoreId) ||
                allowedKioskIds.Contains(alert.KioskId));
        }

        return query;
    }
}
