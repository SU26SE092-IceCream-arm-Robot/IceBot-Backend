using Application.Identity.Workforce.Staff;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using WebAPI.Authorization;

namespace WebAPI.Controllers.Identity;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/management/organizations/{organizationId:guid}/workforce/staff")]
[Authorize]
public sealed class ManagementStaffWorkforceController(
    ListStaffWorkforceQueryHandler listHandler,
    GetStaffWorkforceQueryHandler getHandler,
    CreateStaffWorkforceCommandHandler createHandler,
    UpdateStaffWorkforceCommandHandler updateHandler,
    UpdateStaffWorkforceScopesCommandHandler scopesHandler,
    ChangeStaffWorkforceLifecycleCommandHandler lifecycleHandler,
    SendStaffWorkforceInvitationCommandHandler invitationHandler) : ControllerBase
{
    [HttpGet]
    [Authorize(Policy = "workforce.staff.read")]
    public async Task<IActionResult> List(Guid organizationId, [FromQuery] string? search, [FromQuery] string? status,
        [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 20, CancellationToken cancellationToken = default)
    {
        var result = await listHandler.HandleAsync(new ListStaffWorkforceQuery { UserContext = User.GetUserContext(), OrganizationId = organizationId, Search = search, Status = status, PageNumber = pageNumber, PageSize = pageSize }, cancellationToken);
        return StatusCode(result.StatusCode, result);
    }

    [HttpGet("{accountId:guid}")]
    [Authorize(Policy = "workforce.staff.read")]
    public async Task<IActionResult> Get(Guid organizationId, Guid accountId, CancellationToken cancellationToken)
    {
        var result = await getHandler.HandleAsync(new GetStaffWorkforceQuery { UserContext = User.GetUserContext(), OrganizationId = organizationId, AccountId = accountId }, cancellationToken);
        return StatusCode(result.StatusCode, result);
    }

    [HttpPost]
    [Authorize(Policy = "workforce.staff.manage")]
    public async Task<IActionResult> Create(Guid organizationId, [FromBody] CreateStaffWorkforceRequest request, [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey, CancellationToken cancellationToken)
    {
        var result = await createHandler.HandleAsync(new CreateStaffWorkforceCommand { UserContext = User.GetUserContext(), OrganizationId = organizationId, ActorAccountId = CurrentAccountId(), IdempotencyKey = idempotencyKey, Request = request }, cancellationToken);
        return StatusCode(result.StatusCode, result);
    }

    [HttpPut("{accountId:guid}")]
    [Authorize(Policy = "workforce.staff.manage")]
    public async Task<IActionResult> Update(Guid organizationId, Guid accountId, [FromBody] UpdateStaffWorkforceRequest request, CancellationToken cancellationToken)
    {
        var result = await updateHandler.HandleAsync(new UpdateStaffWorkforceCommand { UserContext = User.GetUserContext(), OrganizationId = organizationId, AccountId = accountId, ActorAccountId = CurrentAccountId(), Request = request }, cancellationToken);
        return StatusCode(result.StatusCode, result);
    }

    [HttpPut("{accountId:guid}/scopes")]
    [Authorize(Policy = "workforce.staff.manage")]
    public async Task<IActionResult> ReplaceScopes(Guid organizationId, Guid accountId, [FromBody] UpdateStaffWorkforceScopesRequest request, CancellationToken cancellationToken)
    {
        var result = await scopesHandler.HandleAsync(new UpdateStaffWorkforceScopesCommand { UserContext = User.GetUserContext(), OrganizationId = organizationId, AccountId = accountId, ActorAccountId = CurrentAccountId(), Request = request }, cancellationToken);
        return StatusCode(result.StatusCode, result);
    }

    [HttpPost("{accountId:guid}/deactivate")]
    [Authorize(Policy = "workforce.staff.manage")]
    public async Task<IActionResult> Deactivate(Guid organizationId, Guid accountId, [FromBody] StaffLifecycleRequest request, CancellationToken cancellationToken)
        => await Lifecycle(organizationId, accountId, request, false, cancellationToken);

    [HttpPost("{accountId:guid}/reactivate")]
    [Authorize(Policy = "workforce.staff.manage")]
    public async Task<IActionResult> Reactivate(Guid organizationId, Guid accountId, [FromBody] StaffLifecycleRequest request, CancellationToken cancellationToken)
        => await Lifecycle(organizationId, accountId, request, true, cancellationToken);

    [HttpPost("{accountId:guid}/invitation")]
    [Authorize(Policy = "workforce.staff.manage")]
    public async Task<IActionResult> Invite(Guid organizationId, Guid accountId, [FromQuery] bool sendEmail = true, CancellationToken cancellationToken = default)
    {
        var result = await invitationHandler.HandleAsync(new SendStaffWorkforceInvitationCommand { UserContext = User.GetUserContext(), OrganizationId = organizationId, AccountId = accountId, ActorAccountId = CurrentAccountId(), SendEmail = sendEmail }, cancellationToken);
        return StatusCode(result.StatusCode, result);
    }

    private async Task<IActionResult> Lifecycle(Guid organizationId, Guid accountId, StaffLifecycleRequest request, bool reactivate, CancellationToken cancellationToken)
    {
        var result = await lifecycleHandler.HandleAsync(new ChangeStaffWorkforceLifecycleCommand { UserContext = User.GetUserContext(), OrganizationId = organizationId, AccountId = accountId, ActorAccountId = CurrentAccountId(), Request = request, Reactivate = reactivate }, cancellationToken);
        return StatusCode(result.StatusCode, result);
    }

    private Guid? CurrentAccountId() => Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var accountId) ? accountId : null;
}
