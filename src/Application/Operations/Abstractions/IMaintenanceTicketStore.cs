using Domain.Operations.Entities;
using Domain.Operations.Enums;

namespace Application.Operations.Abstractions;

public interface IMaintenanceTicketStore
{
    Task<int> CountAsync(
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
        CancellationToken cancellationToken = default);

    Task<List<MaintenanceTicket>> ListAsync(
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
        CancellationToken cancellationToken = default);

    Task<MaintenanceTicket?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task AddAsync(MaintenanceTicket ticket, CancellationToken cancellationToken = default);

    Task<bool> TicketNumberExistsAsync(string ticketNumber, CancellationToken cancellationToken = default);

    Task<bool> ValidateKioskScopeAsync(Guid organizationId, Guid storeId, Guid kioskId, CancellationToken cancellationToken = default);

    Task<bool> DeviceBelongsToKioskAsync(Guid deviceId, Guid kioskId, CancellationToken cancellationToken = default);

    Task<bool> OrderBelongsToScopeAsync(Guid orderId, Guid organizationId, Guid storeId, Guid kioskId, CancellationToken cancellationToken = default);

    Task<bool> DeviceEventBelongsToKioskAsync(Guid deviceEventId, Guid kioskId, CancellationToken cancellationToken = default);

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

    Task<T> ExecuteInTransactionAsync<T>(Func<CancellationToken, Task<T>> action, CancellationToken cancellationToken = default);
}
