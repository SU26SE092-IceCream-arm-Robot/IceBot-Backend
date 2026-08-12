using Domain.Tenants.Entities;

namespace Application.Tenants.Abstractions;

public interface IOrganizationStore
{
    Task<Organization?> GetByIdAsync(Guid id, bool asNoTracking = true, CancellationToken cancellationToken = default);

    Task<Organization?> GetByCodeAsync(string code, bool asNoTracking = true, CancellationToken cancellationToken = default);

    Task<List<Organization>> ListAsync(string? search, string? status, int pageNumber, int pageSize, CancellationToken cancellationToken = default);

    Task<List<Organization>> ListByIdsAsync(IEnumerable<Guid> ids, string? search, string? status, int pageNumber, int pageSize, CancellationToken cancellationToken = default);

    Task<int> CountAsync(string? search, string? status, CancellationToken cancellationToken = default);

    Task<int> CountByIdsAsync(IEnumerable<Guid> ids, string? search, string? status, CancellationToken cancellationToken = default);

    Task<bool> ExistsByCodeAsync(string code, CancellationToken cancellationToken = default);

    Task<OrganizationStatusTransition?> GetStatusTransitionByIdempotencyKeyAsync(
        Guid organizationId,
        string idempotencyKey,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<OrganizationStatusTransition>> ListStatusTransitionsAsync(
        Guid organizationId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<OrganizationStatusTransition>> ListDueSessionRevocationTransitionsAsync(
        DateTimeOffset now,
        int batchSize,
        CancellationToken cancellationToken = default);

    Task<OrganizationStatusTransition?> GetStatusTransitionByIdAsync(
        Guid transitionId,
        bool asNoTracking = true,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Guid>> ListAccountIdsWithOrganizationScopeAsync(
        Guid organizationId,
        CancellationToken cancellationToken = default);

    Task AddAsync(Organization organization, CancellationToken cancellationToken = default);

    Task AddStatusTransitionAsync(OrganizationStatusTransition transition, CancellationToken cancellationToken = default);

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

    Task<T> ExecuteInTransactionAsync<T>(Func<Task<T>> operation, CancellationToken cancellationToken = default);
}
