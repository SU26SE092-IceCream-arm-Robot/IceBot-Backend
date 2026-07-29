using Domain.Identity.Entities;
using Domain.Identity.Enums;

namespace Application.Identity.Abstractions
{
    public interface IRefreshTokenStore
    {
        Task<RefreshToken?> GetByTokenHashAsync(string tokenHash, bool asNoTracking = true, CancellationToken cancellationToken = default);
        Task<List<RefreshToken>> ListActiveByAccountIdAsync(Guid accountId, CancellationToken cancellationToken = default);
        Task AddAsync(RefreshToken refreshToken, CancellationToken cancellationToken = default);
        Task<AccountStatus?> GetAccountStatusAsync(Guid accountId, CancellationToken cancellationToken = default);
        Task AcquireAccountSessionLockAsync(Guid accountId, CancellationToken cancellationToken = default);
        Task AcquireTokenLockAsync(string tokenHash, CancellationToken cancellationToken = default);
        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
        Task<T> ExecuteInTransactionAsync<T>(Func<Task<T>> operation, CancellationToken cancellationToken = default);
    }
}
