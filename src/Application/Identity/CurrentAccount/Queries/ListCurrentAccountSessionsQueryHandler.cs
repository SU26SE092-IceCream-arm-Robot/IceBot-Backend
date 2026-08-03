using Application.Identity.Abstractions;
using Application.Identity.CurrentAccount.Results;
using Application.Identity.CurrentAccount.Support;
using Application.Shared.Wrappers;

namespace Application.Identity.CurrentAccount.Queries;

public sealed class ListCurrentAccountSessionsQueryHandler(IRefreshTokenStore refreshTokens)
{
    public async Task<ApiResult<CurrentAccountSessionsResult>> HandleAsync(
        ListCurrentAccountSessionsQuery query,
        CancellationToken cancellationToken = default)
    {
        if (query.AccountId == Guid.Empty)
        {
            return ApiResult<CurrentAccountSessionsResult>.Fail("A valid account id is required.");
        }

        var sessions = await refreshTokens.ListActiveByAccountIdAsync(query.AccountId, cancellationToken);
        var result = sessions
            .OrderByDescending(session => session.CreatedAt)
            .Select(session => new CurrentAccountSessionResult
            {
                SessionId = session.Id,
                IsCurrentSession = session.Id == query.CurrentSessionId,
                CreatedAt = session.CreatedAt,
                ExpiresAt = session.ExpiresAt,
                IpAddress = session.CreatedByIp,
                UserAgent = session.CreatedByUserAgent,
                DeviceName = SessionDeviceNameResolver.Resolve(session.CreatedByUserAgent)
            })
            .ToList();

        return ApiResult<CurrentAccountSessionsResult>.Success(new CurrentAccountSessionsResult
        {
            CurrentSessionId = query.CurrentSessionId,
            Sessions = result
        });
    }
}
