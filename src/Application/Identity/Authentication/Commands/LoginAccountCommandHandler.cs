using Application.Identity.Abstractions;
using Application.Identity.Authentication.Results;
using Application.Shared.Wrappers;

namespace Application.Identity.Authentication.Commands;

public sealed class LoginAccountCommandHandler
{
    private readonly IAccountAuthenticationService _authenticationService;

    public LoginAccountCommandHandler(IAccountAuthenticationService authenticationService)
    {
        _authenticationService = authenticationService;
    }

    public Task<ApiResult<AuthenticatedAccountResult>> HandleAsync(
        LoginAccountCommand command,
        CancellationToken cancellationToken = default)
    {
        return _authenticationService.LoginAsync(command.Request, command.IpAddress, command.UserAgent);
    }
}
