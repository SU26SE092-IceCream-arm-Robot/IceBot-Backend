using Application.Identity.Abstractions;
using Application.Identity.Authentication.Results;
using Application.Shared.Wrappers;

namespace Application.Identity.Authentication.Commands;

public sealed class RefreshAccessTokenCommandHandler
{
    private readonly IAccountAuthenticationService _authenticationService;

    public RefreshAccessTokenCommandHandler(IAccountAuthenticationService authenticationService)
    {
        _authenticationService = authenticationService;
    }

    public Task<ApiResult<AuthenticatedAccountResult>> HandleAsync(
        RefreshAccessTokenCommand command,
        CancellationToken cancellationToken = default)
    {
        return _authenticationService.RefreshAsync(command.Request, command.IpAddress, command.UserAgent);
    }
}
