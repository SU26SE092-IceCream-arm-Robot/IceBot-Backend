using Application.Shared.Exceptions;
using Application.Tenants.Stores.Requests;
using Application.Tenants.Stores.Services;
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
[Route("api/v{version:apiVersion}/management")]
public class ManagementStoresController : ControllerBase
{
    private readonly StoreManagementService _storeManagement;

    public ManagementStoresController(StoreManagementService storeManagement)
    {
        _storeManagement = storeManagement;
    }

    [HttpGet("stores")]
    [Authorize(Policy = "stores.view")]
    public async Task<IActionResult> ListStores(
        [FromQuery] Guid? organizationId,
        [FromQuery] string? status,
        [FromQuery] string? search,
        CancellationToken cancellationToken)
    {
        var context = User.GetUserContext();
        var result = await _storeManagement.ListStoresAsync(context, organizationId, status, search, cancellationToken);
        return StatusCode(result.StatusCode, result);
    }

    [HttpGet("stores/{storeId:guid}")]
    [Authorize(Policy = "stores.view")]
    public async Task<IActionResult> GetStore(
        Guid storeId,
        CancellationToken cancellationToken)
    {
        var context = User.GetUserContext();
        var result = await _storeManagement.GetStoreAsync(context, storeId, cancellationToken);
        return StatusCode(result.StatusCode, result);
    }

    [HttpPost("organizations/{organizationId:guid}/stores")]
    [Authorize(Policy = "stores.manage")]
    public async Task<IActionResult> CreateStore(
        Guid organizationId,
        [FromBody] CreateStoreRequest request,
        CancellationToken cancellationToken)
    {
        EnsureValidModel();
        var context = User.GetUserContext();
        var result = await _storeManagement.CreateStoreAsync(context, organizationId, request, cancellationToken);
        return StatusCode(result.StatusCode, result);
    }

    [HttpPut("stores/{storeId:guid}")]
    [Authorize(Policy = "stores.update")]
    public async Task<IActionResult> UpdateStore(
        Guid storeId,
        [FromBody] UpdateStoreRequest request,
        CancellationToken cancellationToken)
    {
        EnsureValidModel();
        var context = User.GetUserContext();
        var result = await _storeManagement.UpdateStoreAsync(context, storeId, request, cancellationToken);
        return StatusCode(result.StatusCode, result);
    }

    [HttpPatch("stores/{storeId:guid}/disable")]
    [Authorize(Policy = "stores.manage")]
    public async Task<IActionResult> DisableStore(
        Guid storeId,
        CancellationToken cancellationToken)
    {
        var context = User.GetUserContext();
        var result = await _storeManagement.DisableStoreAsync(context, storeId, cancellationToken);
        return StatusCode(result.StatusCode, result);
    }

    [HttpPatch("stores/{storeId:guid}/activate")]
    [Authorize(Policy = "stores.manage")]
    public async Task<IActionResult> ActivateStore(
        Guid storeId,
        CancellationToken cancellationToken)
    {
        var context = User.GetUserContext();
        var result = await _storeManagement.ActivateStoreAsync(context, storeId, cancellationToken);
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
