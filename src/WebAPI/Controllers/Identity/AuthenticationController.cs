using Application.Identity.Authentication.Commands;
using Application.Identity.Authentication.Requests;
using Application.Identity.Invitations.Commands;
using Application.Identity.Invitations.Requests;
using Application.Identity.PasswordReset.Commands;
using Application.Identity.PasswordReset.Requests;
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
        private readonly LoginAccountCommandHandler _loginHandler;
        private readonly GoogleLoginCommandHandler _googleLoginHandler;
        private readonly RefreshAccessTokenCommandHandler _refreshHandler;
        private readonly RevokeRefreshTokenCommandHandler _revokeHandler;
        private readonly RevokeCurrentAccountTokensCommandHandler _revokeAllHandler;
        private readonly RequestPasswordResetCommandHandler _requestPasswordResetHandler;
        private readonly ResetPasswordCommandHandler _resetPasswordHandler;
        private readonly AcceptInvitationCommandHandler _acceptInvitationHandler;

        public AuthenticationController(
            LoginAccountCommandHandler loginHandler,
            GoogleLoginCommandHandler googleLoginHandler,
            RefreshAccessTokenCommandHandler refreshHandler,
            RevokeRefreshTokenCommandHandler revokeHandler,
            RevokeCurrentAccountTokensCommandHandler revokeAllHandler,
            RequestPasswordResetCommandHandler requestPasswordResetHandler,
            ResetPasswordCommandHandler resetPasswordHandler,
            AcceptInvitationCommandHandler acceptInvitationHandler)
        {
            _loginHandler = loginHandler;
            _googleLoginHandler = googleLoginHandler;
            _refreshHandler = refreshHandler;
            _revokeHandler = revokeHandler;
            _revokeAllHandler = revokeAllHandler;
            _requestPasswordResetHandler = requestPasswordResetHandler;
            _resetPasswordHandler = resetPasswordHandler;
            _acceptInvitationHandler = acceptInvitationHandler;
        }

        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<IActionResult> Login([FromBody] LoginAccountRequest request)
        {
            var command = new LoginAccountCommand
            {
                Request = request,
                IpAddress = GetRemoteIpAddress(),
                UserAgent = GetUserAgent()
            };
            var result = await _loginHandler.HandleAsync(command);
            return StatusCode(result.StatusCode, result);
        }

        [HttpPost("google")]
        [AllowAnonymous]
        public async Task<IActionResult> GoogleLogin([FromBody] ExternalLoginRequest request)
        {
            var command = new GoogleLoginCommand
            {
                Request = request,
                IpAddress = GetRemoteIpAddress(),
                UserAgent = GetUserAgent()
            };
            var result = await _googleLoginHandler.HandleAsync(command);
            return StatusCode(result.StatusCode, result);
        }

        [HttpPost("refresh")]
        [AllowAnonymous]
        public async Task<IActionResult> Refresh([FromBody] RefreshAccessTokenRequest request)
        {
            var command = new RefreshAccessTokenCommand
            {
                Request = request,
                IpAddress = GetRemoteIpAddress(),
                UserAgent = GetUserAgent()
            };
            var result = await _refreshHandler.HandleAsync(command);
            return StatusCode(result.StatusCode, result);
        }

        [HttpPost("revoke")]
        [AllowAnonymous]
        public async Task<IActionResult> Revoke([FromBody] RevokeRefreshTokenRequest request)
        {
            var command = new RevokeRefreshTokenCommand
            {
                Request = request,
                IpAddress = GetRemoteIpAddress(),
                UserAgent = GetUserAgent()
            };
            var result = await _revokeHandler.HandleAsync(command);
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

            var command = new RevokeCurrentAccountTokensCommand
            {
                AccountId = parsedAccountId,
                Request = request,
                IpAddress = GetRemoteIpAddress(),
                UserAgent = GetUserAgent()
            };
            var result = await _revokeAllHandler.HandleAsync(command);
            return StatusCode(result.StatusCode, result);
        }

        [HttpPost("forgot-password")]
        [AllowAnonymous]
        public async Task<IActionResult> ForgotPassword(
            [FromBody] RequestPasswordResetRequest request,
            CancellationToken cancellationToken)
        {
            var command = new RequestPasswordResetCommand
            {
                Request = request,
                IpAddress = GetRemoteIpAddress(),
                UserAgent = GetUserAgent()
            };

            var result = await _requestPasswordResetHandler.HandleAsync(command, cancellationToken);
            return StatusCode(result.StatusCode, result);
        }

        [HttpPost("reset-password")]
        [AllowAnonymous]
        public async Task<IActionResult> ResetPassword(
            [FromBody] ResetPasswordRequest request,
            CancellationToken cancellationToken)
        {
            var command = new ResetPasswordCommand
            {
                Request = request,
                IpAddress = GetRemoteIpAddress(),
                UserAgent = GetUserAgent()
            };

            var result = await _resetPasswordHandler.HandleAsync(command, cancellationToken);

            return StatusCode(result.StatusCode, result);
        }

        [HttpPost("accept-invitation")]
        [AllowAnonymous]
        public async Task<IActionResult> AcceptInvitation(
            [FromBody] AcceptInvitationRequest request,
            CancellationToken cancellationToken)
        {
            var command = new AcceptInvitationCommand
            {
                Request = request,
                IpAddress = GetRemoteIpAddress(),
                UserAgent = GetUserAgent()
            };

            var result = await _acceptInvitationHandler.HandleAsync(command, cancellationToken);

            return StatusCode(result.StatusCode, result);
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
