using Application.Identity.InternalAccounts.Requests;
using Application.Identity.InternalAccounts.Services;
using Application.Shared.Exceptions;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace WebAPI.Controllers.Identity
{
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/management/accounts")]
    [Authorize(Policy = "accounts.manage")]
    public class ManagementAccountsController : ControllerBase
    {
        private readonly InternalAccountService _internalAccounts;

        public ManagementAccountsController(InternalAccountService internalAccounts)
        {
            _internalAccounts = internalAccounts;
        }

        [HttpGet]
        public async Task<IActionResult> ListInternalAccounts(
            [FromQuery] string? search,
            [FromQuery] string? status,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 20,
            CancellationToken cancellationToken = default)
        {
            var result = await _internalAccounts.ListInternalAccountsAsync(
                search,
                status,
                pageNumber,
                pageSize,
                cancellationToken);

            return StatusCode(result.StatusCode, result);
        }

        [HttpGet("{accountId:guid}")]
        public async Task<IActionResult> GetInternalAccount(
            Guid accountId,
            CancellationToken cancellationToken)
        {
            var result = await _internalAccounts.GetInternalAccountAsync(accountId, cancellationToken);
            return StatusCode(result.StatusCode, result);
        }

        [HttpPost]
        public async Task<IActionResult> CreateInternalAccount(
            [FromBody] CreateInternalAccountRequest request,
            CancellationToken cancellationToken)
        {
            EnsureValidModel();

            var createdByAccountId = GetCurrentAccountId();
            var result = await _internalAccounts.CreateInternalAccountAsync(request, createdByAccountId, cancellationToken);
            return StatusCode(result.StatusCode, result);
        }

        [HttpPut("{accountId:guid}")]
        public async Task<IActionResult> UpdateInternalAccount(
            Guid accountId,
            [FromBody] UpdateInternalAccountRequest request,
            CancellationToken cancellationToken)
        {
            EnsureValidModel();

            var result = await _internalAccounts.UpdateInternalAccountAsync(
                accountId,
                request,
                GetCurrentAccountId(),
                cancellationToken);

            return StatusCode(result.StatusCode, result);
        }

        [HttpPatch("{accountId:guid}/disable")]
        public async Task<IActionResult> DisableInternalAccount(
            Guid accountId,
            CancellationToken cancellationToken)
        {
            var result = await _internalAccounts.DisableInternalAccountAsync(
                accountId,
                GetCurrentAccountId(),
                cancellationToken);

            return StatusCode(result.StatusCode, result);
        }

        [HttpPut("{accountId:guid}/password")]
        public async Task<IActionResult> SetPassword(
            Guid accountId,
            [FromBody] SetInternalAccountPasswordRequest request,
            CancellationToken cancellationToken)
        {
            EnsureValidModel();

            var result = await _internalAccounts.SetPasswordAsync(
                accountId,
                request,
                GetCurrentAccountId(),
                cancellationToken);

            return StatusCode(result.StatusCode, result);
        }

        [HttpPost("{accountId:guid}/roles")]
        public async Task<IActionResult> AssignRole(
            Guid accountId,
            [FromBody] AccountRoleScopeRequest request,
            CancellationToken cancellationToken)
        {
            EnsureValidModel();

            var result = await _internalAccounts.AssignRoleAsync(
                accountId,
                request,
                GetCurrentAccountId(),
                cancellationToken);

            return StatusCode(result.StatusCode, result);
        }

        private Guid? GetCurrentAccountId()
        {
            var accountId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return Guid.TryParse(accountId, out var parsedAccountId) ? parsedAccountId : null;
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
}
