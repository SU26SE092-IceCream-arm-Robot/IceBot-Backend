using Domain.Identity.Entities;

namespace Application.Identity.Workforce.Staff;

public interface IStaffWorkforceStore
{
    Task<Account?> GetByIdAsync(
        Guid accountId,
        bool asNoTracking = true,
        CancellationToken cancellationToken = default);

    Task<Role?> GetRoleByCodeAsync(string code, CancellationToken cancellationToken = default);
    Task<List<Account>> ListStaffAsync(
        string? search,
        string? status,
        Guid organizationId,
        IReadOnlySet<Guid> allowedOrganizationIds,
        IReadOnlySet<Guid> allowedStoreIds,
        IReadOnlySet<Guid> allowedKioskIds,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<int> CountStaffAsync(
        string? search,
        string? status,
        Guid organizationId,
        IReadOnlySet<Guid> allowedOrganizationIds,
        IReadOnlySet<Guid> allowedStoreIds,
        IReadOnlySet<Guid> allowedKioskIds,
        CancellationToken cancellationToken = default);

    Task<bool> ExistsByEmailOrUserNameAsync(string email, string userName, CancellationToken cancellationToken = default);
    Task<bool> GoogleEmailExistsAsync(string googleEmail, Guid? excludedAccountId = null, CancellationToken cancellationToken = default);
    Task AddAsync(Account account, CancellationToken cancellationToken = default);
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    Task<T> ExecuteTransactionAsync<T>(Func<Task<T>> operation, CancellationToken cancellationToken = default);
    Task AcquireCreateLockAsync(Guid organizationId, string idempotencyKey, CancellationToken cancellationToken = default);
    Task AcquireCreateIdentityLocksAsync(IReadOnlyCollection<string> identifiers, CancellationToken cancellationToken = default);
    Task AcquireAccountLockAsync(Guid accountId, CancellationToken cancellationToken = default);
    Task<StaffWorkforceCreateReplay?> GetCreateReplayAsync(
        Guid organizationId,
        string idempotencyKey,
        CancellationToken cancellationToken = default);

    Task AddCreateReplayAsync(StaffWorkforceCreateReplay replay, CancellationToken cancellationToken = default);
    Task<StaffWorkforceLifecycleTransition?> GetLifecycleTransitionByIdempotencyKeyAsync(
        Guid organizationId,
        string idempotencyKey,
        CancellationToken cancellationToken = default);

    Task AddLifecycleTransitionAsync(StaffWorkforceLifecycleTransition transition, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Guid>> ListDisabledStaffWithActiveSessionsAsync(int batchSize, CancellationToken cancellationToken = default);
}
