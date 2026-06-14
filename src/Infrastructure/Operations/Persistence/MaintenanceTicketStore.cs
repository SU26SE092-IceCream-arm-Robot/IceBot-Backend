using Domain.Operations.Entities;
using Domain.Operations.Enums;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Operations.Persistence;

public sealed class MaintenanceTicketStore : IMaintenanceTicketStore
{
    private readonly IceBotDbContext _dbContext;

    public MaintenanceTicketStore(IceBotDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    private IQueryable<MaintenanceTicket> ApplyFilters(
        MaintenanceTicketStatus? status,
        MaintenancePriority? priority,
        Guid? organizationId,
        Guid? storeId,
        Guid? kioskId,
        Guid? assignedToAccountId,
        Guid? createdByAccountId,
        DateTimeOffset? fromDate,
        DateTimeOffset? toDate,
        bool isSystemAdmin,
        IReadOnlyCollection<Guid> allowedOrganizationIds,
        IReadOnlyCollection<Guid> allowedStoreIds,
        IReadOnlyCollection<Guid> allowedKioskIds)
    {
        var query = _dbContext.MaintenanceTickets.AsNoTracking();

        if (status.HasValue)
        {
            query = query.Where(t => t.Status == status.Value);
        }

        if (priority.HasValue)
        {
            query = query.Where(t => t.Priority == priority.Value);
        }

        if (organizationId.HasValue)
        {
            query = query.Where(t => t.OrganizationId == organizationId.Value);
        }

        if (storeId.HasValue)
        {
            query = query.Where(t => t.StoreId == storeId.Value);
        }

        if (kioskId.HasValue)
        {
            query = query.Where(t => t.KioskId == kioskId.Value);
        }

        if (assignedToAccountId.HasValue)
        {
            query = query.Where(t => t.AssignedToAccountId == assignedToAccountId.Value);
        }

        if (createdByAccountId.HasValue)
        {
            query = query.Where(t => t.CreatedByAccountId == createdByAccountId.Value);
        }

        if (fromDate.HasValue)
        {
            query = query.Where(t => t.ReportedAt >= fromDate.Value);
        }

        if (toDate.HasValue)
        {
            query = query.Where(t => t.ReportedAt <= toDate.Value);
        }

        if (!isSystemAdmin)
        {
            var allowedOrgs = allowedOrganizationIds ?? Array.Empty<Guid>();
            var allowedStores = allowedStoreIds ?? Array.Empty<Guid>();
            var allowedKiosks = allowedKioskIds ?? Array.Empty<Guid>();

            query = query.Where(t =>
                allowedOrgs.Contains(t.OrganizationId) ||
                allowedStores.Contains(t.StoreId) ||
                allowedKiosks.Contains(t.KioskId));
        }

        return query;
    }

    public Task<int> CountAsync(
        MaintenanceTicketStatus? status,
        MaintenancePriority? priority,
        Guid? organizationId,
        Guid? storeId,
        Guid? kioskId,
        Guid? assignedToAccountId,
        Guid? createdByAccountId,
        DateTimeOffset? fromDate,
        DateTimeOffset? toDate,
        bool isSystemAdmin,
        IReadOnlyCollection<Guid> allowedOrganizationIds,
        IReadOnlyCollection<Guid> allowedStoreIds,
        IReadOnlyCollection<Guid> allowedKioskIds,
        CancellationToken cancellationToken = default)
    {
        return ApplyFilters(
            status,
            priority,
            organizationId,
            storeId,
            kioskId,
            assignedToAccountId,
            createdByAccountId,
            fromDate,
            toDate,
            isSystemAdmin,
            allowedOrganizationIds,
            allowedStoreIds,
            allowedKioskIds)
            .CountAsync(cancellationToken);
    }

    public Task<List<MaintenanceTicket>> ListAsync(
        MaintenanceTicketStatus? status,
        MaintenancePriority? priority,
        Guid? organizationId,
        Guid? storeId,
        Guid? kioskId,
        Guid? assignedToAccountId,
        Guid? createdByAccountId,
        DateTimeOffset? fromDate,
        DateTimeOffset? toDate,
        bool isSystemAdmin,
        IReadOnlyCollection<Guid> allowedOrganizationIds,
        IReadOnlyCollection<Guid> allowedStoreIds,
        IReadOnlyCollection<Guid> allowedKioskIds,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        return ApplyFilters(
            status,
            priority,
            organizationId,
            storeId,
            kioskId,
            assignedToAccountId,
            createdByAccountId,
            fromDate,
            toDate,
            isSystemAdmin,
            allowedOrganizationIds,
            allowedStoreIds,
            allowedKioskIds)
            .OrderByDescending(t => t.ReportedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
    }

    public Task<MaintenanceTicket?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return _dbContext.MaintenanceTickets
            .Include(t => t.Kiosk)
            .Include(t => t.Device)
            .Include(t => t.Order)
            .Include(t => t.DeviceEvent)
            .Include(t => t.AssignedToAccount)
            .Include(t => t.CreatedByAccount)
            .FirstOrDefaultAsync(t => t.Id == id, cancellationToken);
    }

    public async Task AddAsync(MaintenanceTicket ticket, CancellationToken cancellationToken = default)
    {
        await _dbContext.MaintenanceTickets.AddAsync(ticket, cancellationToken);
    }

    public Task<bool> TicketNumberExistsAsync(string ticketNumber, CancellationToken cancellationToken = default)
    {
        return _dbContext.MaintenanceTickets.AnyAsync(t => t.TicketNumber == ticketNumber, cancellationToken);
    }

    public Task<bool> ValidateKioskScopeAsync(Guid organizationId, Guid storeId, Guid kioskId, CancellationToken cancellationToken = default)
    {
        return _dbContext.Kiosks.AnyAsync(k => k.Id == kioskId && k.StoreId == storeId && k.OrganizationId == organizationId, cancellationToken);
    }

    public Task<bool> DeviceBelongsToKioskAsync(Guid deviceId, Guid kioskId, CancellationToken cancellationToken = default)
    {
        return _dbContext.Devices.AnyAsync(d => d.Id == deviceId && d.KioskId == kioskId, cancellationToken);
    }

    public Task<bool> OrderBelongsToScopeAsync(Guid orderId, Guid organizationId, Guid storeId, Guid kioskId, CancellationToken cancellationToken = default)
    {
        return _dbContext.Orders.AnyAsync(o => o.Id == orderId && o.OrganizationId == organizationId && o.StoreId == storeId && o.KioskId == kioskId, cancellationToken);
    }

    public Task<bool> DeviceEventBelongsToKioskAsync(Guid deviceEventId, Guid kioskId, CancellationToken cancellationToken = default)
    {
        return _dbContext.DeviceEvents.AnyAsync(de => de.Id == deviceEventId && de.KioskId == kioskId, cancellationToken);
    }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<T> ExecuteInTransactionAsync<T>(Func<CancellationToken, Task<T>> action, CancellationToken cancellationToken = default)
    {
        await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            var result = await action(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return result;
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }
}
