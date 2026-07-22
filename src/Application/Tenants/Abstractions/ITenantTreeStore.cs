using Domain.Tenants.Entities;

namespace Application.Tenants.Abstractions;

public interface ITenantTreeStore
{
    Task<IReadOnlyList<Organization>> ListOrganizationsAsync(
        bool includeInactive,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Store>> ListStoresAsync(
        bool includeInactive,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Kiosk>> ListKiosksAsync(
        bool includeInactive,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Store>> ListStoresByIdsAsync(
        IEnumerable<Guid> storeIds,
        bool includeInactive,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Kiosk>> ListKiosksByIdsAsync(
        IEnumerable<Guid> kioskIds,
        bool includeInactive,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Organization>> ListOrganizationsByIdsAsync(
        IEnumerable<Guid> organizationIds,
        bool includeInactive,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Store>> ListStoresForScopeAsync(
        IEnumerable<Guid> organizationIds,
        IEnumerable<Guid> storeIds,
        bool includeInactive,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Kiosk>> ListKiosksForScopeAsync(
        IEnumerable<Guid> organizationIds,
        IEnumerable<Guid> storeIds,
        IEnumerable<Guid> kioskIds,
        bool includeInactive,
        CancellationToken cancellationToken = default);
}
