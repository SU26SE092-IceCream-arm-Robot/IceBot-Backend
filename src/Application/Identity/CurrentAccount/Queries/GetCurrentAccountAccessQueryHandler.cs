using Application.Identity.Access.Mapping;
using Application.Identity.Access.Results;
using Application.Shared.Wrappers;

namespace Application.Identity.CurrentAccount.Queries;

public sealed class GetCurrentAccountAccessQueryHandler
{
    public Task<ApiResult<AccountAccessResult>> HandleAsync(
        GetCurrentAccountAccessQuery query,
        CancellationToken cancellationToken = default)
    {
        var result = AccountAccessResultMapper.FromCurrentUserContext(
            query.UserContext,
            query.RoleCodes,
            query.RoleScopeClaims);

        return Task.FromResult(ApiResult<AccountAccessResult>.Success(result, "Current access retrieved successfully."));
    }
}
