using Application.Tenants.Organizations.Commands;
using Application.Tenants.Organizations.Queries;
using Application.Tenants.Organizations.Requests;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebAPI.Authorization;

namespace WebAPI.Controllers.Tenants;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/management/organizations")]
public class ManagementOrganizationsController : ControllerBase
{
    private readonly ListOrganizationsQueryHandler _listOrganizations;
    private readonly GetOrganizationQueryHandler _getOrganization;
    private readonly ListOrganizationStatusHistoryQueryHandler _statusHistory;
    private readonly CreateOrganizationCommandHandler _createOrganization;
    private readonly UpdateOrganizationCommandHandler _updateOrganization;
    private readonly OrganizationLifecycleTransitionCommandHandler _lifecycleTransition;

    public ManagementOrganizationsController(
        ListOrganizationsQueryHandler listOrganizations,
        GetOrganizationQueryHandler getOrganization,
        ListOrganizationStatusHistoryQueryHandler statusHistory,
        CreateOrganizationCommandHandler createOrganization,
        UpdateOrganizationCommandHandler updateOrganization,
        OrganizationLifecycleTransitionCommandHandler lifecycleTransition)
    {
        _listOrganizations = listOrganizations;
        _getOrganization = getOrganization;
        _statusHistory = statusHistory;
        _createOrganization = createOrganization;
        _updateOrganization = updateOrganization;
        _lifecycleTransition = lifecycleTransition;
    }

    [HttpGet]
    [Authorize(Policy = "organizations.view")]
    public async Task<IActionResult> ListOrganizations(
        [FromQuery] string? search,
        [FromQuery] string? status,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var context = User.GetUserContext();
        var query = new ListOrganizationsQuery
        {
            UserContext = context,
            Search = search,
            Status = status,
            PageNumber = pageNumber,
            PageSize = pageSize
        };
        var result = await _listOrganizations.HandleAsync(query, cancellationToken);
        return StatusCode(result.StatusCode, result);
    }

    [HttpGet("{organizationId:guid}")]
    [Authorize(Policy = "organizations.view")]
    public async Task<IActionResult> GetOrganization(
        Guid organizationId,
        CancellationToken cancellationToken)
    {
        var context = User.GetUserContext();
        var query = new GetOrganizationQuery
        {
            UserContext = context,
            OrganizationId = organizationId
        };
        var result = await _getOrganization.HandleAsync(query, cancellationToken);
        return StatusCode(result.StatusCode, result);
    }

    [HttpPost]
    [Authorize(Policy = "organizations.manage")]
    public async Task<IActionResult> CreateOrganization(
        [FromBody] CreateOrganizationRequest request,
        CancellationToken cancellationToken)
    {
        var context = User.GetUserContext();
        var command = new CreateOrganizationCommand
        {
            UserContext = context,
            Request = request
        };
        var result = await _createOrganization.HandleAsync(command, cancellationToken);
        return StatusCode(result.StatusCode, result);
    }

    [HttpPut("{organizationId:guid}")]
    [Authorize(Policy = "organizations.update")]
    public async Task<IActionResult> UpdateOrganization(
        Guid organizationId,
        [FromBody] UpdateOrganizationRequest request,
        CancellationToken cancellationToken)
    {
        var context = User.GetUserContext();
        var command = new UpdateOrganizationCommand
        {
            UserContext = context,
            OrganizationId = organizationId,
            Request = request
        };
        var result = await _updateOrganization.HandleAsync(command, cancellationToken);
        return StatusCode(result.StatusCode, result);
    }

    [HttpPost("{organizationId:guid}/suspend")]
    [Authorize(Policy = "organizations.manage")]
    public async Task<IActionResult> SuspendOrganization(
        Guid organizationId,
        [FromBody] OrganizationLifecycleTransitionRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _lifecycleTransition.HandleAsync(new OrganizationLifecycleTransitionCommand
        {
            UserContext = User.GetUserContext(),
            OrganizationId = organizationId,
            Action = OrganizationLifecycleAction.Suspend,
            Request = request
        }, cancellationToken);
        return StatusCode(result.StatusCode, result);
    }

    [HttpGet("{organizationId:guid}/status-history")]
    [Authorize(Policy = "organizations.manage")]
    public async Task<IActionResult> ListOrganizationStatusHistory(
        Guid organizationId,
        CancellationToken cancellationToken)
    {
        var result = await _statusHistory.HandleAsync(User.GetUserContext(), organizationId, cancellationToken);
        return StatusCode(result.StatusCode, result);
    }

    [HttpPost("{organizationId:guid}/resume")]
    [Authorize(Policy = "organizations.manage")]
    public async Task<IActionResult> ResumeOrganization(
        Guid organizationId,
        [FromBody] OrganizationLifecycleTransitionRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _lifecycleTransition.HandleAsync(new OrganizationLifecycleTransitionCommand
        {
            UserContext = User.GetUserContext(),
            OrganizationId = organizationId,
            Action = OrganizationLifecycleAction.Resume,
            Request = request
        }, cancellationToken);
        return StatusCode(result.StatusCode, result);
    }

    [HttpPost("{organizationId:guid}/deactivate")]
    [Authorize(Policy = "organizations.manage")]
    public async Task<IActionResult> DeactivateOrganization(
        Guid organizationId,
        [FromBody] OrganizationLifecycleTransitionRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _lifecycleTransition.HandleAsync(new OrganizationLifecycleTransitionCommand
        {
            UserContext = User.GetUserContext(),
            OrganizationId = organizationId,
            Action = OrganizationLifecycleAction.Deactivate,
            Request = request
        }, cancellationToken);
        return StatusCode(result.StatusCode, result);
    }

    [HttpPost("{organizationId:guid}/reactivate")]
    [Authorize(Policy = "organizations.manage")]
    public async Task<IActionResult> ReactivateOrganization(
        Guid organizationId,
        [FromBody] OrganizationLifecycleTransitionRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _lifecycleTransition.HandleAsync(new OrganizationLifecycleTransitionCommand
        {
            UserContext = User.GetUserContext(),
            OrganizationId = organizationId,
            Action = OrganizationLifecycleAction.Reactivate,
            Request = request
        }, cancellationToken);
        return StatusCode(result.StatusCode, result);
    }
}
