using Domain.Identity.Entities;

namespace Application.Identity.Abstractions
{
    public interface IIdentityAccountStore
    {
        Task<Account?> GetByIdAsync(Guid accountId, bool asNoTracking = true, CancellationToken cancellationToken = default);
        Task<Account?> GetByEmailOrUserNameAsync(string emailOrUserName, bool asNoTracking = true, CancellationToken cancellationToken = default);
        Task<Account?> GetByGoogleEmailAsync(string googleEmail, bool asNoTracking = true, CancellationToken cancellationToken = default);
        Task<Account?> GetByGoogleSubjectIdAsync(string googleSubjectId, bool asNoTracking = true, CancellationToken cancellationToken = default);
        Task<Role?> GetRoleByCodeAsync(string code, CancellationToken cancellationToken = default);
        Task<List<Role>> ListActiveRolesAsync(CancellationToken cancellationToken = default);
        Task<List<Account>> ListAsync(
            string? search,
            string? status,
            Guid organizationId,
            bool isSystemAdmin,
            IReadOnlySet<Guid> allowedOrganizationIds,
            IReadOnlySet<Guid> allowedStoreIds,
            IReadOnlySet<Guid> allowedKioskIds,
            int pageNumber,
            int pageSize,
            CancellationToken cancellationToken = default);

        Task<int> CountAsync(
            string? search,
            string? status,
            Guid organizationId,
            bool isSystemAdmin,
            IReadOnlySet<Guid> allowedOrganizationIds,
            IReadOnlySet<Guid> allowedStoreIds,
            IReadOnlySet<Guid> allowedKioskIds,
            CancellationToken cancellationToken = default);
        Task<List<Account>> ListStaffAsync(
            string? search, string? status, Guid organizationId,
            IReadOnlySet<Guid> allowedOrganizationIds,
            IReadOnlySet<Guid> allowedStoreIds,
            IReadOnlySet<Guid> allowedKioskIds,
            int pageNumber, int pageSize, CancellationToken cancellationToken = default);
        Task<int> CountStaffAsync(
            string? search, string? status, Guid organizationId,
            IReadOnlySet<Guid> allowedOrganizationIds,
            IReadOnlySet<Guid> allowedStoreIds,
            IReadOnlySet<Guid> allowedKioskIds, CancellationToken cancellationToken = default);
        Task<bool> ExistsByEmailOrUserNameAsync(string email, string userName, CancellationToken cancellationToken = default);
        Task<bool> EmailExistsForOtherAccountAsync(Guid accountId, string email, CancellationToken cancellationToken = default);
        Task<bool> GoogleEmailExistsAsync(string googleEmail, Guid? excludedAccountId = null, CancellationToken cancellationToken = default);
        Task<bool> UserNameExistsAsync(string userName, CancellationToken cancellationToken = default);
        Task AddAsync(Account account, CancellationToken cancellationToken = default);
        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
        Task<T> ExecuteInTransactionAsync<T>(Func<Task<T>> operation, CancellationToken cancellationToken = default);
        Task<T> ExecuteStaffWorkforceTransactionAsync<T>(Func<Task<T>> operation, CancellationToken cancellationToken = default);
        Task AcquireStaffWorkforceCreateLockAsync(Guid organizationId, string idempotencyKey, CancellationToken cancellationToken = default);
        Task AcquireStaffWorkforceAccountLockAsync(Guid accountId, CancellationToken cancellationToken = default);
        Task<StaffWorkforceCreateReplay?> GetStaffWorkforceCreateReplayAsync(Guid organizationId, string idempotencyKey, CancellationToken cancellationToken = default);
        Task AddStaffWorkforceCreateReplayAsync(StaffWorkforceCreateReplay replay, CancellationToken cancellationToken = default);
        Task<StaffWorkforceLifecycleTransition?> GetStaffWorkforceLifecycleTransitionByIdempotencyKeyAsync(Guid organizationId, string idempotencyKey, CancellationToken cancellationToken = default);
        Task AddStaffWorkforceLifecycleTransitionAsync(StaffWorkforceLifecycleTransition transition, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<Guid>> ListDisabledStaffWithActiveSessionsAsync(int batchSize, CancellationToken cancellationToken = default);
    }
}
