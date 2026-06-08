using Application.Shared.Exceptions;
using Application.Tenants.Organizations.Requests;
using Application.Tenants.Organizations.Services;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using WebAPI.Authorization;

namespace WebAPI.Controllers.Tenants;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/management/organizations")]
public class ManagementOrganizationsController : ControllerBase
{
    private readonly OrganizationManagementService _organizationManagement;

    public ManagementOrganizationsController(OrganizationManagementService organizationManagement)
    {
        _organizationManagement = organizationManagement;
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
        var result = await _organizationManagement.ListOrganizationsAsync(
            context,
            search,
            status,
            pageNumber,
            pageSize,
            cancellationToken);

        return StatusCode(result.StatusCode, result);
    }

    [HttpGet("{organizationId:guid}")]
    [Authorize(Policy = "organizations.view")]
    public async Task<IActionResult> GetOrganization(
        Guid organizationId,
        CancellationToken cancellationToken)
    {
        var context = User.GetUserContext();
        var result = await _organizationManagement.GetOrganizationAsync(context, organizationId, cancellationToken);
        return StatusCode(result.StatusCode, result);
    }

    [HttpPost]
    [Authorize(Policy = "organizations.manage")]
    public async Task<IActionResult> CreateOrganization(
        [FromBody] CreateOrganizationRequest request,
        CancellationToken cancellationToken)
    {
        EnsureValidModel();
        var context = User.GetUserContext();
        var result = await _organizationManagement.CreateOrganizationAsync(context, request, cancellationToken);
        return StatusCode(result.StatusCode, result);
    }

    [HttpPut("{organizationId:guid}")]
    [Authorize(Policy = "organizations.update")]
    public async Task<IActionResult> UpdateOrganization(
        Guid organizationId,
        [FromBody] UpdateOrganizationRequest request,
        CancellationToken cancellationToken)
    {
        EnsureValidModel();
        var context = User.GetUserContext();
        var result = await _organizationManagement.UpdateOrganizationAsync(context, organizationId, request, cancellationToken);
        return StatusCode(result.StatusCode, result);
    }

    [HttpPatch("{organizationId:guid}/disable")]
    [Authorize(Policy = "organizations.manage")]
    public async Task<IActionResult> DisableOrganization(
        Guid organizationId,
        CancellationToken cancellationToken)
    {
        var context = User.GetUserContext();
        var result = await _organizationManagement.DisableOrganizationAsync(context, organizationId, cancellationToken);
        return StatusCode(result.StatusCode, result);
    }

    [HttpPatch("{organizationId:guid}/activate")]
    [Authorize(Policy = "organizations.manage")]
    public async Task<IActionResult> ActivateOrganization(
        Guid organizationId,
        CancellationToken cancellationToken)
    {
        var context = User.GetUserContext();
        var result = await _organizationManagement.ActivateOrganizationAsync(context, organizationId, cancellationToken);
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
}
