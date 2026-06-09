using Application.Identity.Abstractions;
using Application.Identity.CurrentAccount.Results;
using Application.Shared.Wrappers;

namespace Application.Identity.CurrentAccount.Queries;

public sealed class GetCurrentAccountQueryHandler
{
    private readonly IIdentityAccountStore _accounts;

    public GetCurrentAccountQueryHandler(IIdentityAccountStore accounts)
    {
        _accounts = accounts;
    }

    public async Task<ApiResult<CurrentAccountResult>> HandleAsync(
        GetCurrentAccountQuery query,
        CancellationToken cancellationToken = default)
    {
        var account = await _accounts.GetByIdAsync(query.AccountId, cancellationToken: cancellationToken);
        return account is null
            ? ApiResult<CurrentAccountResult>.Fail("Account not found.", 404)
            : ApiResult<CurrentAccountResult>.Success(CurrentAccountResultMapper.ToResult(account));
    }
}
