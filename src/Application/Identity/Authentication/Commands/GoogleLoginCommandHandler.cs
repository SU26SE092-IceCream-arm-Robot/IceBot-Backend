using Application.Identity.Abstractions;
using Application.Identity.Authentication.Results;
using Application.Shared.Wrappers;

namespace Application.Identity.Authentication.Commands;

public sealed class GoogleLoginCommandHandler
{
    private readonly IAccountAuthenticationService _authenticationService;

    public GoogleLoginCommandHandler(IAccountAuthenticationService authenticationService)
    {
        _authenticationService = authenticationService;
    }

    public Task<ApiResult<AuthenticatedAccountResult>> HandleAsync(
        GoogleLoginCommand command,
        CancellationToken cancellationToken = default)
    {
        return _authenticationService.LoginWithExternalProviderAsync(command.Request, command.IpAddress, command.UserAgent);
    }
}
