using Application.Identity.Abstractions;
using Application.Identity.Authentication.Requests;
using Application.Identity.Authentication.Results;
using Application.Identity.Tokens.Services;
using Application.Shared.Wrappers;
using Domain.Identity.Enums;
using Microsoft.Extensions.Logging;
using System.Text.RegularExpressions;

namespace Application.Identity.Authentication.Services
{
    public class AccountAuthenticationService : IAccountAuthenticationService
    {
        private readonly IIdentityAccountStore _accounts;
        private readonly AccountTokenService _tokenService;
        private readonly ILogger<AccountAuthenticationService> _logger;
        private readonly IExternalIdentityProvider _externalAuth;
        private readonly IPasswordHasher _passwordHasher;

        public AccountAuthenticationService(
            IIdentityAccountStore accounts,
            AccountTokenService tokenService,
            ILogger<AccountAuthenticationService> logger,
            IExternalIdentityProvider externalAuth,
            IPasswordHasher passwordHasher)
        {
            _accounts = accounts;
            _tokenService = tokenService;
            _logger = logger;
            _externalAuth = externalAuth;
            _passwordHasher = passwordHasher;
        }

        public async Task<ApiResult<AuthenticatedAccountResult>> LoginAsync(
            LoginAccountRequest loginDto,
            string? ipAddress = null,
            string? userAgent = null)
        {
            if (string.IsNullOrWhiteSpace(loginDto.EmailOrUsername) || string.IsNullOrWhiteSpace(loginDto.Password))
            {
                return ApiResult<AuthenticatedAccountResult>.Fail("Invalid login data.");
            }

            var login = NormalizeEmailOrUserName(loginDto.EmailOrUsername);
            var account = await _accounts.GetByEmailOrUserNameAsync(login, asNoTracking: false);

            if (account is null)
            {
                _logger.LogWarning("Login failed for unknown account '{Login}'.", login);
                return ApiResult<AuthenticatedAccountResult>.Fail("Invalid credentials.", 401);
            }

            if (account.Status != AccountStatus.Active)
            {
                return ApiResult<AuthenticatedAccountResult>.Fail("Account is not active.", 403);
            }

            if (!account.LocalLoginEnabled)
            {
                return ApiResult<AuthenticatedAccountResult>.Fail("Local login is not enabled for this account.", 403);
            }

            if (account.LockedUntil is not null && account.LockedUntil > DateTimeOffset.UtcNow)
            {
                return ApiResult<AuthenticatedAccountResult>.Fail("Account is temporarily locked.", 423);
            }

            var passwordHash = account.Password?.Value;
            if (string.IsNullOrWhiteSpace(passwordHash) ||
                !_passwordHasher.VerifyPassword(loginDto.Password, passwordHash))
            {
                account.FailedLoginCount += 1;
                if (account.FailedLoginCount >= 5)
                {
                    account.LockedUntil = DateTimeOffset.UtcNow.AddMinutes(15);
                }

                await _accounts.SaveChangesAsync();
                _logger.LogWarning("Invalid credentials for AccountId: {AccountId}.", account.Id);
                return ApiResult<AuthenticatedAccountResult>.Fail("Invalid credentials.", 401);
            }

            account.FailedLoginCount = 0;
            account.LockedUntil = null;
            account.LastLoginAt = DateTimeOffset.UtcNow;
            await _accounts.SaveChangesAsync();

            return await _tokenService.IssueAsync(account, ipAddress, userAgent);
        }

        public Task<ApiResult<AuthenticatedAccountResult>> RefreshAsync(
            RefreshAccessTokenRequest request,
            string? ipAddress = null,
            string? userAgent = null)
            => _tokenService.RefreshAsync(request.RefreshToken, ipAddress, userAgent);

        public async Task<ApiResult<AuthenticatedAccountResult>> LoginWithExternalProviderAsync(
            ExternalLoginRequest dto,
            string? ipAddress = null,
            string? userAgent = null)
        {
            if (string.IsNullOrWhiteSpace(dto.IdToken))
            {
                return ApiResult<AuthenticatedAccountResult>.Fail("External id token is required.");
            }

            try
            {
                var externalUser = await _externalAuth.ValidateTokenAsync(dto.IdToken);

                if (string.IsNullOrWhiteSpace(externalUser.Email) || !externalUser.EmailVerified)
                {
                    return ApiResult<AuthenticatedAccountResult>.Fail("External account must include a verified email.");
                }

                var email = NormalizeEmail(externalUser.Email);
                var account = await _accounts.GetByGoogleSubjectIdAsync(externalUser.ExternalId, asNoTracking: false)
                              ?? await _accounts.GetByGoogleEmailAsync(email, asNoTracking: false);

                if (account is null)
                {
                    return ApiResult<AuthenticatedAccountResult>.Fail("Account is not allowed to use Google login.", 403);
                }
                else if (account.Status != AccountStatus.Active)
                {
                    return ApiResult<AuthenticatedAccountResult>.Fail("Account is not active.", 403);
                }
                else if (!account.GoogleLoginEnabled)
                {
                    return ApiResult<AuthenticatedAccountResult>.Fail("Google login is not enabled for this account.", 403);
                }

                if (!string.Equals(account.GoogleEmail, email, StringComparison.OrdinalIgnoreCase))
                {
                    return ApiResult<AuthenticatedAccountResult>.Fail("Google account email is not allowed for this account.", 403);
                }

                if (!string.IsNullOrWhiteSpace(account.GoogleSubjectId) &&
                    !string.Equals(account.GoogleSubjectId, externalUser.ExternalId, StringComparison.Ordinal))
                {
                    return ApiResult<AuthenticatedAccountResult>.Fail("Google identity does not match the account binding.", 403);
                }

                if (string.IsNullOrWhiteSpace(account.GoogleSubjectId))
                {
                    account.GoogleSubjectId = externalUser.ExternalId;
                }

                account.LastLoginAt = DateTimeOffset.UtcNow;
                await _accounts.SaveChangesAsync();
                return await _tokenService.IssueAsync(account, ipAddress, userAgent);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "External auth failed.");
                return ApiResult<AuthenticatedAccountResult>.Fail("Invalid external token.", 401);
            }
        }

        private static string NormalizeEmailOrUserName(string value)
        {
            var trimmed = value.Trim();
            return trimmed.Contains('@', StringComparison.Ordinal)
                ? NormalizeEmail(trimmed)
                : NormalizeUserName(trimmed);
        }

        private static string NormalizeEmail(string value) => value.Trim().ToLowerInvariant();

        private static string NormalizeUserName(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return $"user{Guid.NewGuid():N}"[..20];
            }

            var normalized = Regex.Replace(value.Trim().ToLowerInvariant(), "[^a-z0-9_.-]", string.Empty);
            if (string.IsNullOrWhiteSpace(normalized))
            {
                normalized = $"user{Guid.NewGuid():N}"[..20];
            }

            return normalized.Length > 50 ? normalized[..50] : normalized;
        }
    }
}
