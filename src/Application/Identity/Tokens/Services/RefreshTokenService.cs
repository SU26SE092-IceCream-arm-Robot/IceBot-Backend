using Application.Identity.Abstractions;
using Domain.Identity.Entities;
using System.Security.Cryptography;
using System.Text;
using Domain.Identity.Enums;

namespace Application.Identity.Tokens.Services
{
    public sealed record RefreshTokenIssue(string Token, RefreshToken Entity);

    public class RefreshTokenService
    {
        private const int RefreshTokenDays = 7;
        private readonly IRefreshTokenStore _refreshTokens;

        public RefreshTokenService(IRefreshTokenStore refreshTokens)
        {
            _refreshTokens = refreshTokens;
        }

        public async Task<RefreshTokenIssue> CreateAsync(Guid accountId, string? ipAddress, string? userAgent = null)
        {
            return await _refreshTokens.ExecuteInTransactionAsync(async () =>
            {
                await _refreshTokens.AcquireAccountSessionLockAsync(accountId);
                var issue = CreateIssue(accountId, ipAddress, userAgent);
                await _refreshTokens.AddAsync(issue.Entity);
                await _refreshTokens.SaveChangesAsync();
                return issue;
            });
        }

        public async Task<(bool Ok, RefreshTokenIssue? NewToken, string? Error)> RotateAsync(
            string token,
            string? ipAddress,
            string? userAgent = null)
        {
            return await _refreshTokens.ExecuteInTransactionAsync<(bool Ok, RefreshTokenIssue? NewToken, string? Error)>(async () =>
            {
                var tokenHash = HashToken(token);
                var observedToken = await _refreshTokens.GetByTokenHashAsync(tokenHash);
                if (observedToken is null)
                {
                    return (false, null, "Invalid refresh token.");
                }
                await _refreshTokens.AcquireAccountSessionLockAsync(observedToken.AccountId);
                await _refreshTokens.AcquireTokenLockAsync(tokenHash);
                var oldToken = await _refreshTokens.GetByTokenHashAsync(tokenHash, asNoTracking: false);
                if (oldToken is null)
                {
                    return (false, null, "Invalid refresh token.");
                }

                if (oldToken.RevokedAt is not null || oldToken.IsUsed)
                {
                    oldToken.ReuseDetectedAt ??= DateTimeOffset.UtcNow;
                    oldToken.RevokedByIp ??= ipAddress;
                    oldToken.RevokedByUserAgent ??= userAgent;
                    oldToken.RevokeReason ??= "Refresh token reuse detected";

                    var activeTokens = await _refreshTokens.ListActiveByAccountIdAsync(oldToken.AccountId);
                    foreach (var activeToken in activeTokens)
                    {
                        activeToken.RevokedAt = DateTimeOffset.UtcNow;
                        activeToken.RevokedByIp = ipAddress;
                        activeToken.RevokedByUserAgent = userAgent;
                        activeToken.RevokeReason = "Refresh token reuse detected";
                    }

                    await _refreshTokens.SaveChangesAsync();
                    return (false, null, "Refresh token reuse detected.");
                }

                if (oldToken.ExpiresAt <= DateTimeOffset.UtcNow)
                {
                    return (false, null, "Refresh token is expired, used, or revoked.");
                }

                var accountStatus = await _refreshTokens.GetAccountStatusAsync(oldToken.AccountId);
                if (accountStatus != AccountStatus.Active)
                {
                    var activeTokens = await _refreshTokens.ListActiveByAccountIdAsync(oldToken.AccountId);
                    foreach (var activeToken in activeTokens)
                    {
                        activeToken.RevokedAt = DateTimeOffset.UtcNow;
                        activeToken.RevokedByIp = ipAddress;
                        activeToken.RevokedByUserAgent = userAgent;
                        activeToken.RevokeReason = "Account is not active";
                    }
                    await _refreshTokens.SaveChangesAsync();
                    return (false, null, "Account is not active.");
                }

                var newToken = CreateIssue(oldToken.AccountId, ipAddress, userAgent);
                oldToken.RevokedAt = DateTimeOffset.UtcNow;
                oldToken.RevokedByIp = ipAddress;
                oldToken.RevokedByUserAgent = userAgent;
                oldToken.RevokeReason = "Rotated";
                oldToken.IsUsed = true;
                oldToken.ReplacedByTokenId = newToken.Entity.Id;

                await _refreshTokens.AddAsync(newToken.Entity);
                await _refreshTokens.SaveChangesAsync();
                return (true, newToken, null);
            });
        }

        public async Task<bool> RevokeByTokenAsync(string token, string? reason, string? ipAddress, string? userAgent = null)
        {
            return await _refreshTokens.ExecuteInTransactionAsync(async () =>
            {
                var tokenHash = HashToken(token);
                var observedToken = await _refreshTokens.GetByTokenHashAsync(tokenHash);
                if (observedToken is null) return false;
                await _refreshTokens.AcquireAccountSessionLockAsync(observedToken.AccountId);
                await _refreshTokens.AcquireTokenLockAsync(tokenHash);
                var refreshToken = await _refreshTokens.GetByTokenHashAsync(tokenHash, asNoTracking: false);
                if (refreshToken is null)
                {
                    return false;
                }

                if (refreshToken.RevokedAt is null)
                {
                    refreshToken.RevokedAt = DateTimeOffset.UtcNow;
                    refreshToken.RevokedByIp = ipAddress;
                    refreshToken.RevokedByUserAgent = userAgent;
                    refreshToken.RevokeReason = reason;
                    await _refreshTokens.SaveChangesAsync();
                }

                return true;
            });
        }

        public async Task<int> RevokeAllForAccountAsync(Guid accountId, string? reason, string? ipAddress, string? userAgent = null)
        {
            return await _refreshTokens.ExecuteInTransactionAsync(async () =>
            {
                await _refreshTokens.AcquireAccountSessionLockAsync(accountId);
                var activeTokens = await _refreshTokens.ListActiveByAccountIdAsync(accountId);
                foreach (var token in activeTokens)
                {
                    token.RevokedAt = DateTimeOffset.UtcNow;
                    token.RevokedByIp = ipAddress;
                    token.RevokedByUserAgent = userAgent;
                    token.RevokeReason = reason;
                }

                await _refreshTokens.SaveChangesAsync();
                return activeTokens.Count;
            });
        }

        private static RefreshTokenIssue CreateIssue(Guid accountId, string? ipAddress, string? userAgent)
        {
            var rawToken = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
            var refreshToken = new RefreshToken
            {
                AccountId = accountId,
                TokenHash = HashToken(rawToken),
                CreatedAt = DateTimeOffset.UtcNow,
                ExpiresAt = DateTimeOffset.UtcNow.AddDays(RefreshTokenDays),
                CreatedByIp = ipAddress,
                CreatedByUserAgent = userAgent
            };

            return new RefreshTokenIssue(rawToken, refreshToken);
        }

        private static string HashToken(string token)
        {
            var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(token));
            return Convert.ToHexString(bytes);
        }
    }
}
