using Application.Identity.Abstractions;
using Application.Identity.CurrentAccount.Results;
using Application.Shared.Wrappers;

namespace Application.Identity.CurrentAccount.Queries;

public sealed class ListCurrentAccountSessionsQueryHandler(IRefreshTokenStore refreshTokens)
{
    public async Task<ApiResult<IReadOnlyList<CurrentAccountSessionResult>>> HandleAsync(
        ListCurrentAccountSessionsQuery query,
        CancellationToken cancellationToken = default)
    {
        if (query.AccountId == Guid.Empty)
        {
            return ApiResult<IReadOnlyList<CurrentAccountSessionResult>>.Fail("A valid account id is required.");
        }

        var sessions = await refreshTokens.ListActiveByAccountIdAsync(query.AccountId, cancellationToken);
        var result = sessions
            .OrderByDescending(session => session.CreatedAt)
            .Select(session => new CurrentAccountSessionResult
            {
                SessionId = session.Id,
                CreatedAt = session.CreatedAt,
                ExpiresAt = session.ExpiresAt,
                IpAddress = session.CreatedByIp,
                UserAgent = session.CreatedByUserAgent
            })
            .ToList();

        return ApiResult<IReadOnlyList<CurrentAccountSessionResult>>.Success(result);
    }
}
