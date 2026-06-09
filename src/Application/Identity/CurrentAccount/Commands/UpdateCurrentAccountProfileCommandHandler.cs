using Application.Identity.Abstractions;
using Application.Identity.CurrentAccount.Results;
using Application.Shared.Wrappers;
using Domain.Identity.Enums;

namespace Application.Identity.CurrentAccount.Commands;

public sealed class UpdateCurrentAccountProfileCommandHandler
{
    private readonly IIdentityAccountStore _accounts;

    public UpdateCurrentAccountProfileCommandHandler(IIdentityAccountStore accounts)
    {
        _accounts = accounts;
    }

    public async Task<ApiResult<CurrentAccountResult>> HandleAsync(
        UpdateCurrentAccountProfileCommand command,
        CancellationToken cancellationToken = default)
    {
        var accountId = command.AccountId;
        var request = command.Request;

        var account = await _accounts.GetByIdAsync(accountId, asNoTracking: false, cancellationToken: cancellationToken);
        if (account is null)
        {
            return ApiResult<CurrentAccountResult>.Fail("Account not found.", 404);
        }

        if (account.Status != AccountStatus.Active)
        {
            return ApiResult<CurrentAccountResult>.Fail("Account is not active.", 403);
        }

        account.FullName = request.FullName is null ? account.FullName : CurrentAccountProfileNormalizer.TrimToNull(request.FullName);
        account.PhoneNumber = request.PhoneNumber is null ? account.PhoneNumber : CurrentAccountProfileNormalizer.TrimToNull(request.PhoneNumber);
        account.Address = request.Address is null ? account.Address : CurrentAccountProfileNormalizer.TrimToNull(request.Address);
        account.Gender = string.IsNullOrWhiteSpace(request.Gender) ? account.Gender : request.Gender.Trim();
        account.ImageUrl = request.ImageUrl is null ? account.ImageUrl : CurrentAccountProfileNormalizer.TrimToNull(request.ImageUrl);
        account.UpdatedAt = DateTimeOffset.UtcNow;
        account.UpdatedByAccountId = account.Id;

        await _accounts.SaveChangesAsync(cancellationToken);
        return ApiResult<CurrentAccountResult>.Success(CurrentAccountResultMapper.ToResult(account), "Profile updated.");
    }
}
