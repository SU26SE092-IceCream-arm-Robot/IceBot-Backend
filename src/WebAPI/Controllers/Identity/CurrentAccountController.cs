using Application.Identity.CurrentAccount.Requests;
using Application.Identity.CurrentAccount.Services;
using Application.Shared.Exceptions;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace WebAPI.Controllers.Identity;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/me")]
[Authorize]
public sealed class CurrentAccountController : ControllerBase
{
    private readonly CurrentAccountService _currentAccount;

    public CurrentAccountController(CurrentAccountService currentAccount)
    {
        _currentAccount = currentAccount;
    }

    [HttpGet]
    public async Task<IActionResult> GetCurrentAccount(CancellationToken cancellationToken)
    {
        var accountId = GetCurrentAccountId();
        var result = await _currentAccount.GetAsync(accountId, cancellationToken);
        return StatusCode(result.StatusCode, result);
    }

    [HttpPut("profile")]
    public async Task<IActionResult> UpdateProfile(
        [FromBody] UpdateCurrentAccountProfileRequest request,
        CancellationToken cancellationToken)
    {
        EnsureValidModel();

        var accountId = GetCurrentAccountId();
        var result = await _currentAccount.UpdateProfileAsync(accountId, request, cancellationToken);
        return StatusCode(result.StatusCode, result);
    }

    [HttpPut("password")]
    public async Task<IActionResult> ChangePassword(
        [FromBody] ChangeCurrentAccountPasswordRequest request,
        CancellationToken cancellationToken)
    {
        EnsureValidModel();

        var accountId = GetCurrentAccountId();
        var result = await _currentAccount.ChangePasswordAsync(
            accountId,
            request,
            HttpContext.Connection.RemoteIpAddress?.ToString(),
            Request.Headers.UserAgent.ToString(),
            cancellationToken);

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
}
