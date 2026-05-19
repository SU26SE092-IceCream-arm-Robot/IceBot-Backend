using Application.Identity.Abstractions;
using Application.Identity.InternalAccounts.Requests;
using Application.Identity.InternalAccounts.Results;
using Application.Identity.Tokens.Services;
using Application.Shared.Wrappers;
using Domain.Identity.Entities;
using Domain.Identity.Enums;
using Domain.Identity.ValueObjects;
using System.Text.RegularExpressions;

namespace Application.Identity.InternalAccounts.Services
{
    public class InternalAccountService
    {
        private readonly IIdentityAccountStore _accounts;
        private readonly IPasswordHasher _passwordHasher;
        private readonly RefreshTokenService _refreshTokens;

        public InternalAccountService(
            IIdentityAccountStore accounts,
            IPasswordHasher passwordHasher,
            RefreshTokenService refreshTokens)
        {
            _accounts = accounts;
            _passwordHasher = passwordHasher;
            _refreshTokens = refreshTokens;
        }

        public async Task<ApiResult<InternalAccountResult>> CreateInternalAccountAsync(
            CreateInternalAccountRequest request,
            Guid? createdByAccountId = null,
            CancellationToken cancellationToken = default)
        {
            var validationError = ValidateRequest(request);
            if (validationError is not null)
            {
                return ApiResult<InternalAccountResult>.Fail(validationError);
            }

            var email = NormalizeEmail(request.Email);
            var userName = NormalizeUserName(request.UserName);

            if (await _accounts.ExistsByEmailOrUserNameAsync(email, userName, cancellationToken))
            {
                return ApiResult<InternalAccountResult>.Fail("Account already exists.", 409);
            }

            var roles = new List<(Role Role, AccountRoleScopeRequest Scope)>();
            foreach (var roleScope in request.Roles)
            {
                var role = await _accounts.GetRoleByCodeAsync(roleScope.RoleCode.Trim(), cancellationToken);
                if (role is null)
                {
                    return ApiResult<InternalAccountResult>.Fail($"Role '{roleScope.RoleCode}' does not exist.", 400);
                }

                roles.Add((role, roleScope));
            }

            var now = DateTimeOffset.UtcNow;
            var account = new Account
            {
                Id = Guid.NewGuid(),
                UserName = userName,
                Email = email,
                FullName = request.FullName?.Trim(),
                PhoneNumber = request.PhoneNumber?.Trim(),
                Address = request.Address?.Trim(),
                Gender = string.IsNullOrWhiteSpace(request.Gender) ? "Other" : request.Gender.Trim(),
                Status = AccountStatus.Active,
                LocalLoginEnabled = request.LocalLoginEnabled,
                GoogleLoginEnabled = request.GoogleLoginEnabled,
                GoogleEmail = request.GoogleLoginEnabled ? NormalizeEmail(request.GoogleEmail!) : null,
                Password = request.LocalLoginEnabled ? HashedPassword.From(_passwordHasher.HashPassword(request.InitialPassword!)) : null,
                CreatedAt = now,
                CreatedByAccountId = createdByAccountId
            };

            foreach (var (role, scope) in roles)
            {
                account.AccountRoles.Add(new AccountRole
                {
                    Id = Guid.NewGuid(),
                    RoleId = role.Id,
                    Role = role,
                    OrganizationId = scope.OrganizationId,
                    StoreId = scope.StoreId,
                    KioskId = scope.KioskId,
                    AssignedAt = now,
                    AssignedByAccountId = createdByAccountId
                });
            }

            await _accounts.AddAsync(account, cancellationToken);
            await _accounts.SaveChangesAsync(cancellationToken);

            return ApiResult<InternalAccountResult>.Success(ToResult(account), "Internal account created.", 201);
        }

        public async Task<ApiResult<InternalAccountResult>> GetInternalAccountAsync(
            Guid accountId,
            CancellationToken cancellationToken = default)
        {
            var account = await _accounts.GetByIdAsync(accountId, cancellationToken: cancellationToken);
            return account is null
                ? ApiResult<InternalAccountResult>.Fail("Account not found.", 404)
                : ApiResult<InternalAccountResult>.Success(ToResult(account));
        }

        public async Task<PagedResult<InternalAccountResult>> ListInternalAccountsAsync(
            string? search,
            string? status,
            int pageNumber,
            int pageSize,
            CancellationToken cancellationToken = default)
        {
            pageNumber = Math.Max(pageNumber, 1);
            pageSize = Math.Clamp(pageSize, 1, 100);

            var totalCount = await _accounts.CountAsync(search, status, cancellationToken);
            var accounts = await _accounts.ListAsync(search, status, pageNumber, pageSize, cancellationToken);
            return PagedResult<InternalAccountResult>.Success(
                accounts.Select(ToResult),
                totalCount,
                pageNumber,
                pageSize);
        }

        public async Task<ApiResult<InternalAccountResult>> UpdateInternalAccountAsync(
            Guid accountId,
            UpdateInternalAccountRequest request,
            Guid? updatedByAccountId = null,
            CancellationToken cancellationToken = default)
        {
            var account = await _accounts.GetByIdAsync(accountId, asNoTracking: false, cancellationToken: cancellationToken);
            if (account is null)
            {
                return ApiResult<InternalAccountResult>.Fail("Account not found.", 404);
            }

            if (!string.IsNullOrWhiteSpace(request.Email))
            {
                var email = NormalizeEmail(request.Email);
                if (await _accounts.EmailExistsForOtherAccountAsync(accountId, email, cancellationToken))
                {
                    return ApiResult<InternalAccountResult>.Fail("Email already belongs to another account.", 409);
                }

                account.Email = email;
            }

            account.FullName = request.FullName?.Trim() ?? account.FullName;
            account.PhoneNumber = request.PhoneNumber?.Trim() ?? account.PhoneNumber;
            account.Address = request.Address?.Trim() ?? account.Address;
            account.Gender = string.IsNullOrWhiteSpace(request.Gender) ? account.Gender : request.Gender.Trim();
            account.Status = request.Status ?? account.Status;

            if (request.LocalLoginEnabled.HasValue)
            {
                if (request.LocalLoginEnabled.Value && account.Password is null)
                {
                    return ApiResult<InternalAccountResult>.Fail("Set a password before enabling local login.");
                }

                account.LocalLoginEnabled = request.LocalLoginEnabled.Value;
            }

            if (request.GoogleLoginEnabled.HasValue)
            {
                if (request.GoogleLoginEnabled.Value && string.IsNullOrWhiteSpace(request.GoogleEmail ?? account.GoogleEmail))
                {
                    return ApiResult<InternalAccountResult>.Fail("Google email is required before enabling Google login.");
                }

                account.GoogleLoginEnabled = request.GoogleLoginEnabled.Value;
            }

            if (!string.IsNullOrWhiteSpace(request.GoogleEmail))
            {
                account.GoogleEmail = NormalizeEmail(request.GoogleEmail);
            }

            account.UpdatedAt = DateTimeOffset.UtcNow;
            account.UpdatedByAccountId = updatedByAccountId;
            await _accounts.SaveChangesAsync(cancellationToken);

            return ApiResult<InternalAccountResult>.Success(ToResult(account), "Internal account updated.");
        }

        public async Task<ApiResult<InternalAccountResult>> DisableInternalAccountAsync(
            Guid accountId,
            Guid? updatedByAccountId = null,
            CancellationToken cancellationToken = default)
        {
            var account = await _accounts.GetByIdAsync(accountId, asNoTracking: false, cancellationToken: cancellationToken);
            if (account is null)
            {
                return ApiResult<InternalAccountResult>.Fail("Account not found.", 404);
            }

            account.Status = AccountStatus.Disabled;
            account.UpdatedAt = DateTimeOffset.UtcNow;
            account.UpdatedByAccountId = updatedByAccountId;
            await _accounts.SaveChangesAsync(cancellationToken);
            await _refreshTokens.RevokeAllForAccountAsync(account.Id, "Account disabled by admin", null);

            return ApiResult<InternalAccountResult>.Success(ToResult(account), "Internal account disabled.");
        }

        public async Task<ApiResult<InternalAccountResult>> SetPasswordAsync(
            Guid accountId,
            SetInternalAccountPasswordRequest request,
            Guid? updatedByAccountId = null,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(request.NewPassword))
            {
                return ApiResult<InternalAccountResult>.Fail("New password is required.");
            }

            var account = await _accounts.GetByIdAsync(accountId, asNoTracking: false, cancellationToken: cancellationToken);
            if (account is null)
            {
                return ApiResult<InternalAccountResult>.Fail("Account not found.", 404);
            }

            account.Password = HashedPassword.From(_passwordHasher.HashPassword(request.NewPassword));
            account.LocalLoginEnabled = request.EnableLocalLogin || account.LocalLoginEnabled;
            account.LockedUntil = null;
            account.FailedLoginCount = 0;
            account.UpdatedAt = DateTimeOffset.UtcNow;
            account.UpdatedByAccountId = updatedByAccountId;
            await _accounts.SaveChangesAsync(cancellationToken);
            await _refreshTokens.RevokeAllForAccountAsync(account.Id, "Password changed by admin", null);

            return ApiResult<InternalAccountResult>.Success(ToResult(account), "Password updated.");
        }

        public async Task<ApiResult<InternalAccountResult>> AssignRoleAsync(
            Guid accountId,
            AccountRoleScopeRequest request,
            Guid? assignedByAccountId = null,
            CancellationToken cancellationToken = default)
        {
            var account = await _accounts.GetByIdAsync(accountId, asNoTracking: false, cancellationToken: cancellationToken);
            if (account is null)
            {
                return ApiResult<InternalAccountResult>.Fail("Account not found.", 404);
            }

            var role = await _accounts.GetRoleByCodeAsync(request.RoleCode.Trim(), cancellationToken);
            if (role is null)
            {
                return ApiResult<InternalAccountResult>.Fail($"Role '{request.RoleCode}' does not exist.", 400);
            }

            var existingRole = account.AccountRoles.FirstOrDefault(accountRole =>
                accountRole.RoleId == role.Id &&
                accountRole.OrganizationId == request.OrganizationId &&
                accountRole.StoreId == request.StoreId &&
                accountRole.KioskId == request.KioskId);

            if (existingRole is not null)
            {
                existingRole.IsActive = true;
                existingRole.AssignedAt = DateTimeOffset.UtcNow;
                existingRole.AssignedByAccountId = assignedByAccountId;
            }
            else
            {
                account.AccountRoles.Add(new AccountRole
                {
                    Id = Guid.NewGuid(),
                    RoleId = role.Id,
                    Role = role,
                    OrganizationId = request.OrganizationId,
                    StoreId = request.StoreId,
                    KioskId = request.KioskId,
                    AssignedAt = DateTimeOffset.UtcNow,
                    AssignedByAccountId = assignedByAccountId
                });
            }

            account.UpdatedAt = DateTimeOffset.UtcNow;
            account.UpdatedByAccountId = assignedByAccountId;
            await _accounts.SaveChangesAsync(cancellationToken);

            return ApiResult<InternalAccountResult>.Success(ToResult(account), "Role assigned.");
        }

        private static string? ValidateRequest(CreateInternalAccountRequest request)
        {
            if (!request.LocalLoginEnabled && !request.GoogleLoginEnabled)
            {
                return "At least one authentication method must be enabled.";
            }

            if (request.LocalLoginEnabled && string.IsNullOrWhiteSpace(request.InitialPassword))
            {
                return "Initial password is required when local login is enabled.";
            }

            if (request.GoogleLoginEnabled && string.IsNullOrWhiteSpace(request.GoogleEmail))
            {
                return "Google email is required when Google login is enabled.";
            }

            if (request.Roles.Count == 0)
            {
                return "At least one role scope is required.";
            }

            return null;
        }

        private static InternalAccountResult ToResult(Account account)
        {
            return new InternalAccountResult
            {
                Id = account.Id,
                UserName = account.UserName,
                Email = account.Email,
                FullName = account.FullName,
                Status = account.Status.ToString(),
                LocalLoginEnabled = account.LocalLoginEnabled,
                GoogleLoginEnabled = account.GoogleLoginEnabled,
                Roles = account.AccountRoles.Select(accountRole => new InternalAccountRoleResult
                {
                    RoleCode = accountRole.Role.Code,
                    OrganizationId = accountRole.OrganizationId,
                    StoreId = accountRole.StoreId,
                    KioskId = accountRole.KioskId
                }).ToList()
            };
        }

        private static string NormalizeEmail(string value) => value.Trim().ToLowerInvariant();

        private static string NormalizeUserName(string value)
        {
            var normalized = Regex.Replace(value.Trim().ToLowerInvariant(), "[^a-z0-9_.-]", string.Empty);
            return normalized.Length > 50 ? normalized[..50] : normalized;
        }
    }
}
