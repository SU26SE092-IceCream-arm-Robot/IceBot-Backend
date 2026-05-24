using Application.Identity.Abstractions;
using Application.Identity.Authentication.Requests;
using Application.Identity.PasswordReset.Requests;
using Application.Identity.PasswordReset.Services;
using Application.Identity.Tokens.Services;
using Application.Shared.Exceptions;
using Application.Shared.Wrappers;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace WebAPI.Controllers.Identity
{
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/authentication")]
    public class AuthenticationController : ControllerBase
    {
        private readonly IAccountAuthenticationService _authenticationService;
        private readonly AccountTokenService _tokenService;
        private readonly PasswordResetService _passwordResetService;

        public AuthenticationController(
            IAccountAuthenticationService authenticationService,
            AccountTokenService tokenService,
            PasswordResetService passwordResetService)
        {
            _authenticationService = authenticationService;
            _tokenService = tokenService;
            _passwordResetService = passwordResetService;
        }

        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<IActionResult> Login([FromBody] LoginAccountRequest request)
        {
            EnsureValidModel();
            var result = await _authenticationService.LoginAsync(request, GetRemoteIpAddress(), GetUserAgent());
            return StatusCode(result.StatusCode, result);
        }

        [HttpPost("google")]
        [AllowAnonymous]
        public async Task<IActionResult> GoogleLogin([FromBody] ExternalLoginRequest request)
        {
            EnsureValidModel();
            var result = await _authenticationService.LoginWithExternalProviderAsync(request, GetRemoteIpAddress(), GetUserAgent());
            return StatusCode(result.StatusCode, result);
        }

        [HttpPost("refresh")]
        [AllowAnonymous]
        public async Task<IActionResult> Refresh([FromBody] RefreshAccessTokenRequest request)
        {
            EnsureValidModel();
            var result = await _authenticationService.RefreshAsync(request, GetRemoteIpAddress(), GetUserAgent());
            return StatusCode(result.StatusCode, result);
        }

        [HttpPost("revoke")]
        [AllowAnonymous]
        public async Task<IActionResult> Revoke([FromBody] RevokeRefreshTokenRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.RefreshToken))
            {
                return StatusCode(400, ApiResult<object>.Fail("Refresh token is required.", 400));
            }

            var revoked = await _tokenService.RevokeByTokenAsync(request.RefreshToken, request.Reason, GetRemoteIpAddress(), GetUserAgent());
            var result = ApiResult<object>.Success(new { revoked }, revoked ? "Revoked" : "Not found", revoked ? 200 : 404);
            return StatusCode(result.StatusCode, result);
        }

        [HttpPost("revoke-all")]
        [Authorize]
        public async Task<IActionResult> RevokeAll([FromBody] RevokeAccountTokensRequest? request)
        {
            var accountId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!Guid.TryParse(accountId, out var parsedAccountId))
            {
                return StatusCode(401, ApiResult<object>.Fail("Unauthorized", 401));
            }

            var revokedCount = await _tokenService.RevokeAllForAccountAsync(parsedAccountId, request?.Reason, GetRemoteIpAddress(), GetUserAgent());
            var result = ApiResult<object>.Success(new { revoked = revokedCount }, "All sessions revoked", 200);
            return StatusCode(result.StatusCode, result);
        }

        [HttpPost("forgot-password")]
        [AllowAnonymous]
        public async Task<IActionResult> ForgotPassword(
            [FromBody] RequestPasswordResetRequest request,
            CancellationToken cancellationToken)
        {
            EnsureValidModel();

            var result = await _passwordResetService.RequestResetAsync(
                request,
                GetRemoteIpAddress(),
                GetUserAgent(),
                cancellationToken);
            return StatusCode(result.StatusCode, result);
        }

        [HttpPost("reset-password")]
        [AllowAnonymous]
        public async Task<IActionResult> ResetPassword(
            [FromBody] ResetPasswordRequest request,
            CancellationToken cancellationToken)
        {
            EnsureValidModel();

            var result = await _passwordResetService.ResetAsync(
                request,
                GetRemoteIpAddress(),
                GetUserAgent(),
                cancellationToken);

            return StatusCode(result.StatusCode, result);
        }

        private void EnsureValidModel()
        {
            if (ModelState.IsValid)
            {
                return;
            }

            var errors = ModelState.ToDictionary(
                item => item.Key,
                item => item.Value?.Errors.FirstOrDefault()?.ErrorMessage ?? "Invalid");

            throw new ValidationException(errors);
        }

        private string? GetRemoteIpAddress()
        {
            return HttpContext.Connection.RemoteIpAddress?.ToString();
        }

        private string? GetUserAgent()
        {
            return Request.Headers.UserAgent.FirstOrDefault();
        }
    }
}
