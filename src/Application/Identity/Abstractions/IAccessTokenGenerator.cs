using Application.Identity.Tokens.Claims;
using Application.Shared.Wrappers;
using Domain.Identity.Enums;

namespace Application.Identity.Abstractions
{
    public interface IAccessTokenGenerator
    {
        ApiResult<string> GenerateAccessToken(
            Guid accountId,
            string accountUserName,
            IReadOnlyCollection<AccountRoleClaim> roles,
            AccountStatus accountStatus);
    }
}
