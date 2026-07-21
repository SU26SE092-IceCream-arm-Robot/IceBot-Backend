using Domain.Tenants.Entities;
using Domain.Tenants.Enums;

namespace Application.Tenants.Abstractions;

public interface IKioskStore
{
    Task<Store?> GetStoreByIdAsync(Guid storeId, CancellationToken cancellationToken = default);
    Task<bool> OrganizationExistsActiveAsync(Guid organizationId, CancellationToken cancellationToken = default);
    Task<bool> StoreExistsActiveAsync(Guid storeId, CancellationToken cancellationToken = default);
    Task<bool> KioskCodeExistsAsync(Guid organizationId, string code, Guid? excludeKioskId = null, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Kiosk>> ListAsync(Guid? organizationId, Guid? storeId, KioskStatus? status, string? search, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Kiosk>> ListAccessibleAsync(
        IEnumerable<Guid> organizationIds,
        IEnumerable<Guid> storeIds,
        IEnumerable<Guid> kioskIds,
        Guid? organizationId,
        Guid? storeId,
        KioskStatus? status,
        string? search,
        CancellationToken cancellationToken = default);
    Task<Kiosk?> GetByIdAsync(Guid kioskId, CancellationToken cancellationToken = default);
    Task<Kiosk?> GetByStoreAndIdAsync(Guid storeId, Guid kioskId, CancellationToken cancellationToken = default);
    Task<bool> HasRunningExecutionAsync(Guid kioskId, CancellationToken cancellationToken = default);
    Task AddOperationalStateTransitionAsync(KioskOperationalStateTransition transition, CancellationToken cancellationToken = default);
    Task<T> ExecuteOperationalStateSerializedAsync<T>(
        Guid kioskId,
        Func<CancellationToken, Task<T>> action,
        CancellationToken cancellationToken = default);
    Task AddAsync(Kiosk kiosk, CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
