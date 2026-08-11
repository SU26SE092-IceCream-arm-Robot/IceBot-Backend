using Application.Identity.Abstractions;
using Domain.Identity.Entities;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System.Data;
using Domain.Identity.Enums;

namespace Infrastructure.Identity.Persistence
{
    public class RefreshTokenStore : IRefreshTokenStore
    {
        private readonly IceBotDbContext _dbContext;

        public RefreshTokenStore(IceBotDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public Task<RefreshToken?> GetByTokenHashAsync(string tokenHash, bool asNoTracking = true, CancellationToken cancellationToken = default)
        {
            var query = asNoTracking
                ? _dbContext.RefreshTokens.AsNoTracking()
                : _dbContext.RefreshTokens;

            return query.FirstOrDefaultAsync(token => token.TokenHash == tokenHash, cancellationToken);
        }

        public Task<RefreshToken?> GetActiveByAccountAndIdAsync(
            Guid accountId,
            Guid sessionId,
            CancellationToken cancellationToken = default) =>
            _dbContext.RefreshTokens.FirstOrDefaultAsync(token =>
                token.Id == sessionId &&
                token.AccountId == accountId &&
                token.RevokedAt == null &&
                !token.IsUsed &&
                token.ExpiresAt > DateTimeOffset.UtcNow,
                cancellationToken);

        public Task<List<RefreshToken>> ListActiveByAccountIdAsync(Guid accountId, CancellationToken cancellationToken = default)
        {
            return _dbContext.RefreshTokens
                .Where(token =>
                    token.AccountId == accountId &&
                    token.RevokedAt == null &&
                    !token.IsUsed &&
                    token.ExpiresAt > DateTimeOffset.UtcNow)
                .ToListAsync(cancellationToken);
        }

        public Task AddAsync(RefreshToken refreshToken, CancellationToken cancellationToken = default)
        {
            return _dbContext.RefreshTokens.AddAsync(refreshToken, cancellationToken).AsTask();
        }

        public Task<AccountStatus?> GetAccountStatusAsync(
            Guid accountId,
            CancellationToken cancellationToken = default) =>
            _dbContext.Accounts.AsNoTracking()
                .Where(account => account.Id == accountId)
                .Select(account => (AccountStatus?)account.Status)
                .SingleOrDefaultAsync(cancellationToken);

        public Task AcquireAccountSessionLockAsync(
            Guid accountId,
            CancellationToken cancellationToken = default) =>
            _dbContext.Database.ExecuteSqlInterpolatedAsync(
                $"SELECT pg_advisory_xact_lock(hashtextextended({$"identity-session:{accountId:D}"}, 0))",
                cancellationToken);

        public Task AcquireTokenLockAsync(
            string tokenHash,
            CancellationToken cancellationToken = default) =>
            _dbContext.Database.ExecuteSqlInterpolatedAsync(
                $"SELECT pg_advisory_xact_lock(hashtextextended({$"identity-refresh-token:{tokenHash}"}, 0))",
                cancellationToken);

        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            return _dbContext.SaveChangesAsync(cancellationToken);
        }

        public async Task<T> ExecuteInTransactionAsync<T>(
            Func<Task<T>> operation,
            CancellationToken cancellationToken = default)
        {
            if (_dbContext.Database.CurrentTransaction is not null)
                return await operation();

            var strategy = _dbContext.Database.CreateExecutionStrategy();
            return await strategy.ExecuteAsync(async () =>
            {
                await using var transaction = await _dbContext.Database.BeginTransactionAsync(
                    IsolationLevel.Serializable,
                    cancellationToken);

                var result = await operation();
                await transaction.CommitAsync(cancellationToken);
                return result;
            });
        }
    }
}
