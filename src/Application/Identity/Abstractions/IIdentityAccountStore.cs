using Domain.Identity.Entities;

namespace Application.Identity.Abstractions
{
    public interface IIdentityAccountStore
    {
        Task<Account?> GetByIdAsync(Guid accountId, bool asNoTracking = true, CancellationToken cancellationToken = default);
        Task<Account?> GetByEmailOrUserNameAsync(string emailOrUserName, bool asNoTracking = true, CancellationToken cancellationToken = default);
        Task<Account?> GetByGoogleSubjectIdAsync(string googleSubjectId, bool asNoTracking = true, CancellationToken cancellationToken = default);
        Task<Role?> GetRoleByCodeAsync(string code, CancellationToken cancellationToken = default);
        Task<List<Account>> ListAsync(string? search, string? status, int pageNumber, int pageSize, CancellationToken cancellationToken = default);
        Task<int> CountAsync(string? search, string? status, CancellationToken cancellationToken = default);
        Task<bool> ExistsByEmailOrUserNameAsync(string email, string userName, CancellationToken cancellationToken = default);
        Task<bool> EmailExistsForOtherAccountAsync(Guid accountId, string email, CancellationToken cancellationToken = default);
        Task<bool> UserNameExistsAsync(string userName, CancellationToken cancellationToken = default);
        Task AddAsync(Account account, CancellationToken cancellationToken = default);
        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    }
}
