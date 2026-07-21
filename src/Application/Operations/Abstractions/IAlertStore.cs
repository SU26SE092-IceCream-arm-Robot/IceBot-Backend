using Domain.Common.Enums;
using Domain.Operations.Entities;
using Domain.Operations.Enums;

namespace Application.Operations.Abstractions;

public interface IAlertStore
{
    Task<T> ExecuteSerializedAsync<T>(
        Guid alertId,
        Func<CancellationToken, Task<T>> action,
        CancellationToken cancellationToken = default);

    Task<int> CountAsync(
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
        CancellationToken cancellationToken = default);

    Task<List<Alert>> ListAsync(
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
        CancellationToken cancellationToken = default);

    Task<Alert?> GetByIdAsync(Guid alertId, CancellationToken cancellationToken = default);
    Task<Alert?> GetAccessibleByIdAsync(
        Guid alertId,
        bool isSystemAdmin,
        IReadOnlyCollection<Guid> allowedOrganizationIds,
        IReadOnlyCollection<Guid> allowedStoreIds,
        IReadOnlyCollection<Guid> allowedKioskIds,
        CancellationToken cancellationToken = default);
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
