using Application.Identity.Abstractions;
using Application.Identity.CurrentAccount.Requests;
using Application.Identity.CurrentAccount.Results;
using Application.Identity.Tokens.Services;
using Application.Shared.Wrappers;
using Domain.Identity.Enums;
using Domain.Identity.ValueObjects;

namespace Application.Identity.CurrentAccount.Services;

public sealed class CurrentAccountService
{
    private readonly IIdentityAccountStore _accounts;
    private readonly IPasswordHasher _passwordHasher;
    private readonly RefreshTokenService _refreshTokens;

    public CurrentAccountService(
        IIdentityAccountStore accounts,
        IPasswordHasher passwordHasher,
        RefreshTokenService refreshTokens)
    {
        _accounts = accounts;
        _passwordHasher = passwordHasher;
        _refreshTokens = refreshTokens;
    }

    public async Task<ApiResult<CurrentAccountResult>> GetAsync(
        Guid accountId,
        CancellationToken cancellationToken = default)
    {
        var account = await _accounts.GetByIdAsync(accountId, cancellationToken: cancellationToken);
        return account is null
            ? ApiResult<CurrentAccountResult>.Fail("Account not found.", 404)
            : ApiResult<CurrentAccountResult>.Success(ToResult(account));
    }

    public async Task<ApiResult<CurrentAccountResult>> UpdateProfileAsync(
        Guid accountId,
        UpdateCurrentAccountProfileRequest request,
        CancellationToken cancellationToken = default)
    {
        var account = await _accounts.GetByIdAsync(accountId, asNoTracking: false, cancellationToken: cancellationToken);
        if (account is null)
        {
            return ApiResult<CurrentAccountResult>.Fail("Account not found.", 404);
        }

        if (account.Status != AccountStatus.Active)
        {
            return ApiResult<CurrentAccountResult>.Fail("Account is not active.", 403);
        }

        account.FullName = request.FullName is null ? account.FullName : TrimToNull(request.FullName);
        account.PhoneNumber = request.PhoneNumber is null ? account.PhoneNumber : TrimToNull(request.PhoneNumber);
        account.Address = request.Address is null ? account.Address : TrimToNull(request.Address);
        account.Gender = string.IsNullOrWhiteSpace(request.Gender) ? account.Gender : request.Gender.Trim();
        account.ImageUrl = request.ImageUrl is null ? account.ImageUrl : TrimToNull(request.ImageUrl);
        account.UpdatedAt = DateTimeOffset.UtcNow;
        account.UpdatedByAccountId = account.Id;

        await _accounts.SaveChangesAsync(cancellationToken);
        return ApiResult<CurrentAccountResult>.Success(ToResult(account), "Profile updated.");
    }

    public async Task<ApiResult<bool>> ChangePasswordAsync(
        Guid accountId,
        ChangeCurrentAccountPasswordRequest request,
        string? ipAddress = null,
        string? userAgent = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.CurrentPassword))
        {
            return ApiResult<bool>.Fail("Current password is required.");
        }

        if (string.IsNullOrWhiteSpace(request.NewPassword))
        {
            return ApiResult<bool>.Fail("New password is required.");
        }

        var account = await _accounts.GetByIdAsync(accountId, asNoTracking: false, cancellationToken: cancellationToken);
        if (account is null)
        {
            return ApiResult<bool>.Fail("Account not found.", 404);
        }

        if (account.Status != AccountStatus.Active)
        {
            return ApiResult<bool>.Fail("Account is not active.", 403);
        }

        if (!account.LocalLoginEnabled || account.Password is null)
        {
            return ApiResult<bool>.Fail("Local password login is not enabled for this account.", 403);
        }

        if (!_passwordHasher.VerifyPassword(request.CurrentPassword, account.Password.Value))
        {
            return ApiResult<bool>.Fail("Current password is incorrect.", 400);
        }

        account.Password = HashedPassword.From(_passwordHasher.HashPassword(request.NewPassword));
        account.FailedLoginCount = 0;
        account.LockedUntil = null;
        account.UpdatedAt = DateTimeOffset.UtcNow;
        account.UpdatedByAccountId = account.Id;

        await _accounts.SaveChangesAsync(cancellationToken);
        await _refreshTokens.RevokeAllForAccountAsync(account.Id, "Password changed by account owner", ipAddress, userAgent);

        return ApiResult<bool>.Success(true, "Password changed.");
    }

    private static CurrentAccountResult ToResult(Domain.Identity.Entities.Account account)
    {
        return new CurrentAccountResult
        {
            Id = account.Id,
            UserName = account.UserName,
            Email = account.Email,
            EmailConfirmed = account.EmailConfirmed,
            FullName = account.FullName,
            ImageUrl = account.ImageUrl,
            PhoneNumber = account.PhoneNumber,
            PhoneNumberConfirmed = account.PhoneNumberConfirmed,
            Address = account.Address,
            Gender = account.Gender,
            Status = account.Status.ToString(),
            LocalLoginEnabled = account.LocalLoginEnabled,
            GoogleLoginEnabled = account.GoogleLoginEnabled,
            GoogleEmail = account.GoogleEmail,
            LastLoginAt = account.LastLoginAt,
            Roles = account.AccountRoles
                .Where(accountRole => accountRole.IsActive)
                .OrderBy(accountRole => accountRole.Role.Priority)
                .Select(accountRole => new CurrentAccountRoleResult
                {
                    RoleCode = accountRole.Role.Code,
                    OrganizationId = accountRole.OrganizationId,
                    StoreId = accountRole.StoreId,
                    KioskId = accountRole.KioskId
                })
                .ToList()
        };
    }

    private static string? TrimToNull(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();
    }
}
