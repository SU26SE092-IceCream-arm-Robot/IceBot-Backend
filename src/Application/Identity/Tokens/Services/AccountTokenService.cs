using Application.Identity.Abstractions;
using Application.Identity.Authentication.Results;
using Application.Identity.Tokens.Claims;
using Application.Shared.Wrappers;
using Domain.Identity.Entities;
using Domain.Identity.Enums;

namespace Application.Identity.Tokens.Services
{
    public class AccountTokenService
    {
        private readonly IAccessTokenGenerator _accessTokenGenerator;
        private readonly RefreshTokenService _refreshTokens;
        private readonly IIdentityAccountStore _accounts;

        public AccountTokenService(
            IAccessTokenGenerator accessTokenGenerator,
            RefreshTokenService refreshTokens,
            IIdentityAccountStore accounts)
        {
            _accessTokenGenerator = accessTokenGenerator;
            _refreshTokens = refreshTokens;
            _accounts = accounts;
        }

        public async Task<ApiResult<AuthenticatedAccountResult>> IssueAsync(
            Account account,
            string? ipAddress = null,
            string? userAgent = null)
        {
            var roles = ResolveRoleClaims(account);
            var refreshToken = await _refreshTokens.CreateAsync(account.Id, ipAddress, userAgent);
            var accessToken = _accessTokenGenerator.GenerateAccessToken(
                account.Id,
                refreshToken.Entity.Id,
                account.UserName,
                roles,
                account.Status);
            if (!accessToken.Succeeded)
            {
                await _refreshTokens.RevokeByTokenAsync(
                    refreshToken.Token,
                    "Access token generation failed",
                    ipAddress,
                    userAgent);
                return ApiResult<AuthenticatedAccountResult>.Fail(accessToken.Message ?? "Failed to create access token.");
            }

            return ApiResult<AuthenticatedAccountResult>.Success(ToAuthenticatedUser(account, roles, accessToken.Data!, refreshToken.Token));
        }

        public async Task<ApiResult<AuthenticatedAccountResult>> RefreshAsync(
            string refreshToken,
            string? ipAddress = null,
            string? userAgent = null)
        {
            var rotation = await _refreshTokens.RotateAsync(refreshToken, ipAddress, userAgent);
            if (!rotation.Ok || rotation.NewToken is null)
            {
                return ApiResult<AuthenticatedAccountResult>.Fail(rotation.Error ?? "Unable to rotate refresh token.", 401);
            }

            var account = await _accounts.GetByIdAsync(rotation.NewToken.Entity.AccountId, asNoTracking: true);
            if (account is null)
            {
                return ApiResult<AuthenticatedAccountResult>.Fail("Account not found for this token.", 401);
            }
            if (account.Status != AccountStatus.Active)
            {
                await _refreshTokens.RevokeAllForAccountAsync(
                    account.Id, "Account is not active", ipAddress, userAgent);
                return ApiResult<AuthenticatedAccountResult>.Fail("Account is not active.", 401);
            }

            var roles = ResolveRoleClaims(account);
            var accessToken = _accessTokenGenerator.GenerateAccessToken(
                account.Id,
                rotation.NewToken.Entity.Id,
                account.UserName,
                roles,
                account.Status);
            if (!accessToken.Succeeded)
            {
                return ApiResult<AuthenticatedAccountResult>.Fail(accessToken.Message ?? "Failed to create access token.");
            }

            return ApiResult<AuthenticatedAccountResult>.Success(ToAuthenticatedUser(account, roles, accessToken.Data!, rotation.NewToken.Token));
        }

        public Task<bool> RevokeByTokenAsync(string refreshToken, string? reason, string? ipAddress = null, string? userAgent = null)
            => _refreshTokens.RevokeByTokenAsync(refreshToken, reason, ipAddress, userAgent);

        public Task<int> RevokeAllForAccountAsync(Guid accountId, string? reason, string? ipAddress = null, string? userAgent = null)
            => _refreshTokens.RevokeAllForAccountAsync(accountId, reason, ipAddress, userAgent);

        private static AuthenticatedAccountResult ToAuthenticatedUser(
            Account account,
            IReadOnlyCollection<AccountRoleClaim> roles,
            string accessToken,
            string refreshToken)
        {
            return new AuthenticatedAccountResult
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                Id = account.Id,
                UserName = account.UserName,
                FullName = account.FullName ?? string.Empty,
                Email = account.Email,
                ImageUrl = account.ImageUrl,
                Roles = roles.Select(role => new AuthenticatedAccountRoleResult
                {
                    RoleCode = role.RoleCode,
                    OrganizationId = role.OrganizationId,
                    StoreId = role.StoreId,
                    KioskId = role.KioskId
                }).ToList(),
                LocalLoginEnabled = account.LocalLoginEnabled,
                GoogleLoginEnabled = account.GoogleLoginEnabled
            };
        }

        private static IReadOnlyCollection<AccountRoleClaim> ResolveRoleClaims(Account account)
        {
            return account.AccountRoles
                       .Where(accountRole => accountRole.IsActive)
                       .OrderBy(accountRole => accountRole.Role.Priority)
                       .Select(accountRole => new AccountRoleClaim(
                           accountRole.Role.Code,
                           accountRole.OrganizationId,
                           accountRole.StoreId,
                           accountRole.KioskId))
                       .ToList();
        }
    }
}
