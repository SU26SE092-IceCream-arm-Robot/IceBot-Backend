using Application.Identity.CurrentAccount.Commands;
using Application.Identity.CurrentAccount.Queries;
using Application.Identity.CurrentAccount.Requests;
using Application.Identity.NotificationDevices.Commands;
using Application.Identity.NotificationDevices.Queries;
using Application.Identity.NotificationDevices.Requests;
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
    private readonly ListCurrentAccountSessionsQueryHandler _listSessions;
    private readonly UpdateCurrentAccountProfileCommandHandler _updateProfile;
    private readonly ChangeCurrentAccountPasswordCommandHandler _changePassword;
    private readonly RegisterCurrentAccountNotificationDeviceCommandHandler _registerNotificationDevice;
    private readonly UnregisterCurrentAccountNotificationDeviceCommandHandler _unregisterNotificationDevice;
    private readonly ListCurrentAccountNotificationDevicesQueryHandler _listNotificationDevices;

    public CurrentAccountController(
        GetCurrentAccountQueryHandler getCurrentAccount,
        GetCurrentAccountAccessQueryHandler getCurrentAccountAccess,
        ListCurrentAccountSessionsQueryHandler listSessions,
        UpdateCurrentAccountProfileCommandHandler updateProfile,
        ChangeCurrentAccountPasswordCommandHandler changePassword,
        RegisterCurrentAccountNotificationDeviceCommandHandler registerNotificationDevice,
        UnregisterCurrentAccountNotificationDeviceCommandHandler unregisterNotificationDevice,
        ListCurrentAccountNotificationDevicesQueryHandler listNotificationDevices)
    {
        _getCurrentAccount = getCurrentAccount;
        _getCurrentAccountAccess = getCurrentAccountAccess;
        _listSessions = listSessions;
        _updateProfile = updateProfile;
        _changePassword = changePassword;
        _registerNotificationDevice = registerNotificationDevice;
        _unregisterNotificationDevice = unregisterNotificationDevice;
        _listNotificationDevices = listNotificationDevices;
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

    [HttpGet("sessions")]
    public async Task<IActionResult> ListSessions(CancellationToken cancellationToken)
    {
        var result = await _listSessions.HandleAsync(
            new ListCurrentAccountSessionsQuery(GetCurrentAccountId()),
            cancellationToken);
        return StatusCode(result.StatusCode, result);
    }

    [HttpGet("notification-devices")]
    public async Task<IActionResult> ListNotificationDevices(CancellationToken cancellationToken)
    {
        var result = await _listNotificationDevices.HandleAsync(
            new ListCurrentAccountNotificationDevicesQuery(GetCurrentAccountId()),
            cancellationToken);
        return StatusCode(result.StatusCode, result);
    }

    [HttpPut("notification-devices/{installationId:guid}")]
    public async Task<IActionResult> RegisterNotificationDevice(
        Guid installationId,
        [FromBody] RegisterCurrentAccountNotificationDeviceRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _registerNotificationDevice.HandleAsync(
            new RegisterCurrentAccountNotificationDeviceCommand
            {
                AccountId = GetCurrentAccountId(),
                InstallationId = installationId,
                Request = request
            },
            cancellationToken);
        return StatusCode(result.StatusCode, result);
    }

    [HttpDelete("notification-devices/{installationId:guid}")]
    public async Task<IActionResult> UnregisterNotificationDevice(
        Guid installationId,
        CancellationToken cancellationToken)
    {
        var result = await _unregisterNotificationDevice.HandleAsync(
            new UnregisterCurrentAccountNotificationDeviceCommand(GetCurrentAccountId(), installationId),
            cancellationToken);
        return result.Succeeded ? NoContent() : StatusCode(result.StatusCode, result);
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
