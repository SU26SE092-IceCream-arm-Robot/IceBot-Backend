using Application.Identity.CurrentAccount.Commands;
using Application.Identity.CurrentAccount.Queries;
using Application.Identity.CurrentAccount.Requests;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using WebAPI.Authorization;

namespace WebAPI.Controllers.Identity;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/me")]
[Authorize]
public sealed class CurrentAccountController : ControllerBase
{
    private readonly GetCurrentAccountQueryHandler _getCurrentAccount;
    private readonly GetCurrentAccountAccessQueryHandler _getCurrentAccountAccess;
    private readonly UpdateCurrentAccountProfileCommandHandler _updateProfile;
    private readonly ChangeCurrentAccountPasswordCommandHandler _changePassword;

    public CurrentAccountController(
        GetCurrentAccountQueryHandler getCurrentAccount,
        GetCurrentAccountAccessQueryHandler getCurrentAccountAccess,
        UpdateCurrentAccountProfileCommandHandler updateProfile,
        ChangeCurrentAccountPasswordCommandHandler changePassword)
    {
        _getCurrentAccount = getCurrentAccount;
        _getCurrentAccountAccess = getCurrentAccountAccess;
        _updateProfile = updateProfile;
        _changePassword = changePassword;
    }

    [HttpGet]
    public async Task<IActionResult> GetCurrentAccount(CancellationToken cancellationToken)
    {
        var accountId = GetCurrentAccountId();
        var query = new GetCurrentAccountQuery
        {
            AccountId = accountId
        };
        var result = await _getCurrentAccount.HandleAsync(query, cancellationToken);
        return StatusCode(result.StatusCode, result);
    }

    [HttpGet("access")]
    public async Task<IActionResult> GetCurrentAccess(CancellationToken cancellationToken)
    {
        var query = new GetCurrentAccountAccessQuery
        {
            UserContext = User.GetUserContext(),
            RoleCodes = User.FindAll(ClaimTypes.Role).Select(claim => claim.Value).ToList(),
            RoleScopeClaims = User.FindAll("role_scope").Select(claim => claim.Value).ToList()
        };
        var result = await _getCurrentAccountAccess.HandleAsync(query, cancellationToken);
        return StatusCode(result.StatusCode, result);
    }

    [HttpPut("profile")]
    public async Task<IActionResult> UpdateProfile(
        [FromBody] UpdateCurrentAccountProfileRequest request,
        CancellationToken cancellationToken)
    {
        var accountId = GetCurrentAccountId();
        var command = new UpdateCurrentAccountProfileCommand
        {
            AccountId = accountId,
            Request = request
        };
        var result = await _updateProfile.HandleAsync(command, cancellationToken);
        return StatusCode(result.StatusCode, result);
    }

    [HttpPut("password")]
    public async Task<IActionResult> ChangePassword(
        [FromBody] ChangeCurrentAccountPasswordRequest request,
        CancellationToken cancellationToken)
    {
        var accountId = GetCurrentAccountId();
        var command = new ChangeCurrentAccountPasswordCommand
        {
            AccountId = accountId,
            Request = request,
            IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString(),
            UserAgent = Request.Headers.UserAgent.ToString()
        };
        var result = await _changePassword.HandleAsync(command, cancellationToken);

        return StatusCode(result.StatusCode, result);
    }

    private Guid GetCurrentAccountId()
    {
        var accountId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (Guid.TryParse(accountId, out var parsedAccountId))
        {
            return parsedAccountId;
        }

        throw new UnauthorizedAccessException("Current account id claim is missing or invalid.");
    }

}
