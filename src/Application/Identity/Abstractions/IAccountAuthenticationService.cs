using Application.Identity.Authentication.Requests;
using Application.Identity.Authentication.Results;
using Application.Shared.Wrappers;

namespace Application.Identity.Abstractions
{
    public interface IAccountAuthenticationService
    {
        Task<ApiResult<AuthenticatedAccountResult>> LoginAsync(LoginAccountRequest request, string? ipAddress = null, string? userAgent = null);
        Task<ApiResult<AuthenticatedAccountResult>> LoginWithExternalProviderAsync(ExternalLoginRequest request, string? ipAddress = null, string? userAgent = null);
        Task<ApiResult<AuthenticatedAccountResult>> RefreshAsync(RefreshAccessTokenRequest request, string? ipAddress = null, string? userAgent = null);
    }
}
