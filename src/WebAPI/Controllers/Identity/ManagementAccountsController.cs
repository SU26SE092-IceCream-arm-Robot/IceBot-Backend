using Application.Identity.InternalAccounts.Commands;
using Application.Identity.InternalAccounts.Queries;
using Application.Identity.InternalAccounts.Requests;
using Application.Identity.Invitations.Requests;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using WebAPI.Authorization;

namespace WebAPI.Controllers.Identity
{
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/management/organizations/{organizationId:guid}/accounts")]
    [Authorize]
    public class ManagementAccountsController : ControllerBase
    {
        private readonly ListInternalAccountsQueryHandler _listInternalAccounts;
        private readonly GetInternalAccountQueryHandler _getInternalAccount;
        private readonly CreateInternalAccountCommandHandler _createInternalAccount;
        private readonly UpdateInternalAccountCommandHandler _updateInternalAccount;
        private readonly DisableInternalAccountCommandHandler _disableInternalAccount;
        private readonly SetInternalAccountPasswordCommandHandler _setPassword;
        private readonly AssignInternalAccountRoleCommandHandler _assignRole;
        private readonly UpdateInternalAccountRolesCommandHandler _updateRoles;
        private readonly GetInternalAccountEffectiveAccessQueryHandler _getEffectiveAccess;
        private readonly CreateInternalAccountInvitationCommandHandler _createInvitation;

        public ManagementAccountsController(
            ListInternalAccountsQueryHandler listInternalAccounts,
            GetInternalAccountQueryHandler getInternalAccount,
            CreateInternalAccountCommandHandler createInternalAccount,
            UpdateInternalAccountCommandHandler updateInternalAccount,
            DisableInternalAccountCommandHandler disableInternalAccount,
            SetInternalAccountPasswordCommandHandler setPassword,
            AssignInternalAccountRoleCommandHandler assignRole,
            UpdateInternalAccountRolesCommandHandler updateRoles,
            GetInternalAccountEffectiveAccessQueryHandler getEffectiveAccess,
            CreateInternalAccountInvitationCommandHandler createInvitation)
        {
            _listInternalAccounts = listInternalAccounts;
            _getInternalAccount = getInternalAccount;
            _createInternalAccount = createInternalAccount;
            _updateInternalAccount = updateInternalAccount;
            _disableInternalAccount = disableInternalAccount;
            _setPassword = setPassword;
            _assignRole = assignRole;
            _updateRoles = updateRoles;
            _getEffectiveAccess = getEffectiveAccess;
            _createInvitation = createInvitation;
        }

        [HttpGet]
        [Authorize(Policy = "accounts.read")]
        public async Task<IActionResult> ListInternalAccounts(
            Guid organizationId,
            [FromQuery] string? search,
            [FromQuery] string? status,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 20,
            CancellationToken cancellationToken = default)
        {
            var query = new ListInternalAccountsQuery
            {
                UserContext = User.GetUserContext(),
                OrganizationId = organizationId,
                Search = search,
                Status = status,
                PageNumber = pageNumber,
                PageSize = pageSize
            };
            var result = await _listInternalAccounts.HandleAsync(query, cancellationToken);
            return StatusCode(result.StatusCode, result);
        }

        [HttpGet("{accountId:guid}")]
        [Authorize(Policy = "accounts.read")]
        public async Task<IActionResult> GetInternalAccount(
            Guid organizationId,
            Guid accountId,
            CancellationToken cancellationToken)
        {
            var query = new GetInternalAccountQuery
            {
                AccountId = accountId,
                OrganizationId = organizationId,
                UserContext = User.GetUserContext()
            };
            var result = await _getInternalAccount.HandleAsync(query, cancellationToken);
            return StatusCode(result.StatusCode, result);
        }

        [HttpPost]
        [Authorize(Policy = "accounts.manage")]
        public async Task<IActionResult> CreateInternalAccount(
            Guid organizationId,
            [FromBody] CreateInternalAccountRequest request,
            CancellationToken cancellationToken)
        {
            var createdByAccountId = GetCurrentAccountId();
            var command = new CreateInternalAccountCommand
            {
                Request = request,
                OrganizationId = organizationId,
                CreatedByAccountId = createdByAccountId,
                UserContext = User.GetUserContext(),
                UserRoles = User.FindAll(ClaimTypes.Role).Select(claim => claim.Value).ToList()
            };
            var result = await _createInternalAccount.HandleAsync(command, cancellationToken);
            return StatusCode(result.StatusCode, result);
        }

        [HttpPut("{accountId:guid}")]
        [Authorize(Policy = "accounts.manage")]
        public async Task<IActionResult> UpdateInternalAccount(
            Guid organizationId,
            Guid accountId,
            [FromBody] UpdateInternalAccountRequest request,
            CancellationToken cancellationToken)
        {
            var command = new UpdateInternalAccountCommand
            {
                AccountId = accountId,
                OrganizationId = organizationId,
                Request = request,
                UpdatedByAccountId = GetCurrentAccountId(),
                UserContext = User.GetUserContext()
            };
            var result = await _updateInternalAccount.HandleAsync(command, cancellationToken);
            return StatusCode(result.StatusCode, result);
        }

        [HttpPatch("{accountId:guid}/disable")]
        [Authorize(Policy = "accounts.manage")]
        public async Task<IActionResult> DisableInternalAccount(
            Guid organizationId,
            Guid accountId,
            CancellationToken cancellationToken)
        {
            var command = new DisableInternalAccountCommand
            {
                AccountId = accountId,
                OrganizationId = organizationId,
                UpdatedByAccountId = GetCurrentAccountId(),
                UserContext = User.GetUserContext()
            };
            var result = await _disableInternalAccount.HandleAsync(command, cancellationToken);
            return StatusCode(result.StatusCode, result);
        }

        [HttpPut("{accountId:guid}/password")]
        [Authorize(Policy = "accounts.manage")]
        public async Task<IActionResult> SetPassword(
            Guid organizationId,
            Guid accountId,
            [FromBody] SetInternalAccountPasswordRequest request,
            CancellationToken cancellationToken)
        {
            var command = new SetInternalAccountPasswordCommand
            {
                AccountId = accountId,
                OrganizationId = organizationId,
                Request = request,
                UpdatedByAccountId = GetCurrentAccountId(),
                UserContext = User.GetUserContext()
            };
            var result = await _setPassword.HandleAsync(command, cancellationToken);
            return StatusCode(result.StatusCode, result);
        }

        [HttpPost("{accountId:guid}/roles")]
        [Authorize(Policy = "accounts.manage")]
        public async Task<IActionResult> AssignRole(
            Guid organizationId,
            Guid accountId,
            [FromBody] AccountRoleScopeRequest request,
            CancellationToken cancellationToken)
        {
            var command = new AssignInternalAccountRoleCommand
            {
                AccountId = accountId,
                OrganizationId = organizationId,
                Request = request,
                AssignedByAccountId = GetCurrentAccountId(),
                UserContext = User.GetUserContext(),
                UserRoles = User.FindAll(ClaimTypes.Role).Select(claim => claim.Value).ToList()
            };
            var result = await _assignRole.HandleAsync(command, cancellationToken);
            return StatusCode(result.StatusCode, result);
        }

        [HttpPut("{accountId:guid}/roles")]
        [Authorize(Policy = "accounts.manage")]
        public async Task<IActionResult> UpdateRoles(
            Guid organizationId,
            Guid accountId,
            [FromBody] UpdateAccountRolesRequest request,
            CancellationToken cancellationToken)
        {
            var command = new UpdateInternalAccountRolesCommand
            {
                AccountId = accountId,
                OrganizationId = organizationId,
                Request = request,
                UpdatedByAccountId = GetCurrentAccountId(),
                UserContext = User.GetUserContext(),
                UserRoles = User.FindAll(ClaimTypes.Role).Select(claim => claim.Value).ToList()
            };
            var result = await _updateRoles.HandleAsync(command, cancellationToken);
            return StatusCode(result.StatusCode, result);
        }

        [HttpGet("{accountId:guid}/effective-access")]
        [Authorize(Policy = "accounts.read")]
        public async Task<IActionResult> GetEffectiveAccess(
            Guid organizationId,
            Guid accountId,
            CancellationToken cancellationToken)
        {
            var query = new GetInternalAccountEffectiveAccessQuery
            {
                AccountId = accountId,
                OrganizationId = organizationId,
                UserContext = User.GetUserContext()
            };
            var result = await _getEffectiveAccess.HandleAsync(query, cancellationToken);
            return StatusCode(result.StatusCode, result);
        }

        [HttpPost("{accountId:guid}/invitation")]
        [Authorize(Policy = "accounts.manage")]
        public async Task<IActionResult> CreateInvitation(
            Guid organizationId,
            Guid accountId,
            [FromBody] CreateAccountInvitationRequest? request,
            CancellationToken cancellationToken)
        {
            var command = new CreateInternalAccountInvitationCommand
            {
                AccountId = accountId,
                OrganizationId = organizationId,
                InvitedByAccountId = GetCurrentAccountId(),
                SendEmail = request?.SendEmail ?? true,
                UserContext = User.GetUserContext()
            };
            var result = await _createInvitation.HandleAsync(command, cancellationToken);
            return StatusCode(result.StatusCode, result);
        }

        private Guid? GetCurrentAccountId()
        {
            var accountId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return Guid.TryParse(accountId, out var parsedAccountId) ? parsedAccountId : null;
        }

    }
}
