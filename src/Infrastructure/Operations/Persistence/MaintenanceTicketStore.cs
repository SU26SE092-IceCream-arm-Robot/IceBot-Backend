using Domain.Devices.Telemetry;
using Application.Operations.Abstractions;
using Application.Operations.MaintenanceTickets.Results;
using Application.Operations.MaintenanceTickets.Rules;
using Domain.Operations.Entities;
using Domain.Operations.Enums;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Application.Tenants.Kiosks.Rules;
using Domain.ProductionExecution.Enums;
using Domain.Sync.Enums;
using Domain.Tenants.Entities;

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

    public Task<MaintenanceKioskScope?> GetKioskScopeAsync(Guid kioskId, CancellationToken cancellationToken = default)
    {
        return _dbContext.Kiosks.AsNoTracking()
            .Where(kiosk => kiosk.Id == kioskId)
            .Select(kiosk => new MaintenanceKioskScope(kiosk.OrganizationId, kiosk.StoreId, kiosk.Id))
            .FirstOrDefaultAsync(cancellationToken);
    }

    public Task<bool> DeviceBelongsToKioskAsync(Guid deviceId, Guid kioskId, CancellationToken cancellationToken = default)
    {
        return _dbContext.Devices.WhereNotDeleted().AnyAsync(d => d.Id == deviceId && d.KioskId == kioskId, cancellationToken);
    }

    public Task<bool> OrderBelongsToScopeAsync(Guid orderId, Guid organizationId, Guid storeId, Guid kioskId, CancellationToken cancellationToken = default)
    {
        return _dbContext.Orders.WhereNotDeleted().AnyAsync(o => o.Id == orderId && o.OrganizationId == organizationId && o.StoreId == storeId && o.KioskId == kioskId, cancellationToken);
    }

    public Task<bool> DeviceEventBelongsToKioskAsync(Guid deviceEventId, Guid kioskId, CancellationToken cancellationToken = default)
    {
        return _dbContext.DeviceEvents.AnyAsync(de => de.Id == deviceEventId && de.KioskId == kioskId, cancellationToken);
    }

    public Task<bool> CanAssignAccountAsync(
        Guid accountId,
        Guid organizationId,
        Guid storeId,
        Guid kioskId,
        CancellationToken cancellationToken = default)
    {
        if (accountId == Guid.Empty)
        {
            return Task.FromResult(false);
        }

        return QueryAssignableAccountRoles(organizationId, storeId, kioskId)
            .AnyAsync(accountRole => accountRole.AccountId == accountId, cancellationToken);
    }

    public async Task<List<MaintenanceAssigneeOptionResult>> ListAssignableAccountsAsync(
        Guid organizationId,
        Guid storeId,
        Guid kioskId,
        CancellationToken cancellationToken = default)
    {
        var rows = await QueryAssignableAccountRoles(organizationId, storeId, kioskId)
            .Select(accountRole => new
            {
                accountRole.AccountId,
                DisplayName = accountRole.Account.FullName ?? accountRole.Account.UserName,
                RoleCode = accountRole.Role.Code
            })
            .ToListAsync(cancellationToken);

        return rows
            .GroupBy(row => new
            {
                row.AccountId,
                row.DisplayName
            })
            .Select(group => new MaintenanceAssigneeOptionResult
            {
                AccountId = group.Key.AccountId,
                DisplayName = group.Key.DisplayName,
                RoleCodes = group
                    .Select(row => row.RoleCode)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .OrderBy(roleCode => roleCode, StringComparer.Ordinal)
                    .ToList()
            })
            .OrderBy(option => option.DisplayName, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private IQueryable<Domain.Identity.Entities.AccountRole> QueryAssignableAccountRoles(
        Guid organizationId,
        Guid storeId,
        Guid kioskId)
    {
        return _dbContext.AccountRoles.AsNoTracking().Where(accountRole =>
            accountRole.IsActive &&
            accountRole.Account.DeletedAt == null &&
            accountRole.Account.Status == Domain.Identity.Enums.AccountStatus.Active &&
            (
                (MaintenanceTicketAccessRules.AssigneeRoles.Contains(accountRole.Role.Code) &&
                 (accountRole.KioskId == kioskId ||
                  (!accountRole.KioskId.HasValue && accountRole.StoreId == storeId) ||
                  (!accountRole.KioskId.HasValue &&
                   !accountRole.StoreId.HasValue &&
                   accountRole.OrganizationId == organizationId)))
            ));
    }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<bool> TrySaveNewTicketAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
            return true;
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException
               { SqlState: PostgresErrorCodes.UniqueViolation,
                 ConstraintName: "IX_MaintenanceTickets_TicketNumber" })
        {
            return false;
        }
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

    public Task AcquireKioskOperationalLockAsync(
        Guid kioskId,
        CancellationToken cancellationToken = default) =>
        _dbContext.Database.ExecuteSqlInterpolatedAsync(
            $"SELECT pg_advisory_xact_lock(hashtextextended({KioskOperationalConcurrency.LockKey(kioskId)}, 0));",
            cancellationToken);

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
}
