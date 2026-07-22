using Application.Identity.Abstractions;
using Application.Identity.InternalAccounts.Results;
using Application.Shared.Wrappers;

namespace Application.Identity.InternalAccounts.Commands;

public sealed class UpdateInternalAccountCommandHandler
{
    private readonly IIdentityAccountStore _accounts;

    public UpdateInternalAccountCommandHandler(IIdentityAccountStore accounts)
    {
        _accounts = accounts;
    }

    public async Task<ApiResult<InternalAccountResult>> HandleAsync(
        UpdateInternalAccountCommand command,
        CancellationToken cancellationToken = default)
    {
        var accountId = command.AccountId;
        var request = command.Request;
        var updatedByAccountId = command.UpdatedByAccountId;

        var account = await _accounts.GetByIdAsync(accountId, asNoTracking: false, cancellationToken: cancellationToken);
        if (account is null)
        {
            return ApiResult<InternalAccountResult>.Fail("Account not found.", 404);
        }

        if (!string.IsNullOrWhiteSpace(request.Email))
        {
            var email = InternalAccountNormalizer.NormalizeEmail(request.Email);
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

        if (request.LocalLoginEnabled.HasValue)
        {
            if (request.LocalLoginEnabled.Value && account.Password is null)
            {
                return ApiResult<InternalAccountResult>.Fail("Set a password before enabling local login.");
            }

            account.LocalLoginEnabled = request.LocalLoginEnabled.Value;
        }

        var googleEmail = !string.IsNullOrWhiteSpace(request.GoogleEmail)
            ? InternalAccountNormalizer.NormalizeEmail(request.GoogleEmail)
            : account.GoogleEmail;

        if (request.GoogleLoginEnabled.HasValue)
        {
            if (request.GoogleLoginEnabled.Value && string.IsNullOrWhiteSpace(googleEmail))
            {
                return ApiResult<InternalAccountResult>.Fail("Google email is required before enabling Google login.");
            }

            account.GoogleLoginEnabled = request.GoogleLoginEnabled.Value;
        }

        if (!string.IsNullOrWhiteSpace(googleEmail) &&
            !string.Equals(account.GoogleEmail, googleEmail, StringComparison.Ordinal))
        {
            if (await _accounts.GoogleEmailExistsAsync(googleEmail, accountId, cancellationToken))
            {
                return ApiResult<InternalAccountResult>.Fail("Google email already belongs to another account.", 409);
            }

            account.GoogleEmail = googleEmail;
            account.GoogleSubjectId = null;
        }

        account.UpdatedAt = DateTimeOffset.UtcNow;
        account.UpdatedByAccountId = updatedByAccountId;
        await _accounts.SaveChangesAsync(cancellationToken);

        return ApiResult<InternalAccountResult>.Success(InternalAccountResultMapper.ToResult(account), "Internal account updated.");
    }
}
