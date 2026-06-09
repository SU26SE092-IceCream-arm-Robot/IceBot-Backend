using Application.Shared.Exceptions;
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
    private readonly CreateOrganizationCommandHandler _createOrganization;
    private readonly UpdateOrganizationCommandHandler _updateOrganization;
    private readonly DisableOrganizationCommandHandler _disableOrganization;
    private readonly ActivateOrganizationCommandHandler _activateOrganization;

    public ManagementOrganizationsController(
        ListOrganizationsQueryHandler listOrganizations,
        GetOrganizationQueryHandler getOrganization,
        CreateOrganizationCommandHandler createOrganization,
        UpdateOrganizationCommandHandler updateOrganization,
        DisableOrganizationCommandHandler disableOrganization,
        ActivateOrganizationCommandHandler activateOrganization)
    {
        _listOrganizations = listOrganizations;
        _getOrganization = getOrganization;
        _createOrganization = createOrganization;
        _updateOrganization = updateOrganization;
        _disableOrganization = disableOrganization;
        _activateOrganization = activateOrganization;
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

    [HttpPatch("{organizationId:guid}/disable")]
    [Authorize(Policy = "organizations.manage")]
    public async Task<IActionResult> DisableOrganization(
        Guid organizationId,
        CancellationToken cancellationToken)
    {
        var context = User.GetUserContext();
        var command = new DisableOrganizationCommand
        {
            UserContext = context,
            OrganizationId = organizationId
        };
        var result = await _disableOrganization.HandleAsync(command, cancellationToken);
        return StatusCode(result.StatusCode, result);
    }

    [HttpPatch("{organizationId:guid}/activate")]
    [Authorize(Policy = "organizations.manage")]
    public async Task<IActionResult> ActivateOrganization(
        Guid organizationId,
        CancellationToken cancellationToken)
    {
        var context = User.GetUserContext();
        var command = new ActivateOrganizationCommand
        {
            UserContext = context,
            OrganizationId = organizationId
        };
        var result = await _activateOrganization.HandleAsync(command, cancellationToken);
        return StatusCode(result.StatusCode, result);
    }
}
