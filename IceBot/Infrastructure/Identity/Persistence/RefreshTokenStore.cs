using Application.Identity.Abstractions;
using Domain.Identity.Entities;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System.Data;

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

        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            return _dbContext.SaveChangesAsync(cancellationToken);
        }

        public async Task<T> ExecuteInTransactionAsync<T>(
            Func<Task<T>> operation,
            CancellationToken cancellationToken = default)
        {
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
