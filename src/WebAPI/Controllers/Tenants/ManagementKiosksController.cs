using Application.Shared.Exceptions;
using Application.Tenants.Kiosks.Requests;
using Application.Tenants.Kiosks.Services;
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
public class ManagementKiosksController : ControllerBase
{
    private readonly KioskManagementService _kioskManagement;

    public ManagementKiosksController(KioskManagementService kioskManagement)
    {
        _kioskManagement = kioskManagement;
    }

    [HttpGet("kiosks")]
    [Authorize(Policy = "kiosks.view")]
    public async Task<IActionResult> ListKiosks(
        [FromQuery] Guid? organizationId,
        [FromQuery] Guid? storeId,
        [FromQuery] string? status,
        [FromQuery] string? search,
        CancellationToken cancellationToken)
    {
        var context = User.GetUserContext();
        var result = await _kioskManagement.ListKiosksAsync(
            context, organizationId, storeId, status, search, cancellationToken);
        return StatusCode(result.StatusCode, result);
    }

    [HttpGet("kiosks/{kioskId:guid}")]
    [Authorize(Policy = "kiosks.view")]
    public async Task<IActionResult> GetKiosk(
        Guid kioskId,
        CancellationToken cancellationToken)
    {
        var context = User.GetUserContext();
        var result = await _kioskManagement.GetKioskAsync(context, kioskId, cancellationToken);
        return StatusCode(result.StatusCode, result);
    }

    [HttpPost("stores/{storeId:guid}/kiosks")]
    [Authorize(Policy = "kiosks.manage")]
    public async Task<IActionResult> CreateKiosk(
        Guid storeId,
        [FromBody] CreateKioskRequest request,
        CancellationToken cancellationToken)
    {
        EnsureValidModel();
        var context = User.GetUserContext();
        var result = await _kioskManagement.CreateKioskAsync(context, storeId, request, cancellationToken);
        return StatusCode(result.StatusCode, result);
    }

    [HttpPut("kiosks/{kioskId:guid}")]
    [Authorize(Policy = "kiosks.update")]
    public async Task<IActionResult> UpdateKiosk(
        Guid kioskId,
        [FromBody] UpdateKioskRequest request,
        CancellationToken cancellationToken)
    {
        EnsureValidModel();
        var context = User.GetUserContext();
        var result = await _kioskManagement.UpdateKioskAsync(context, kioskId, request, cancellationToken);
        return StatusCode(result.StatusCode, result);
    }

    [HttpPatch("kiosks/{kioskId:guid}/status")]
    [Authorize(Policy = "kiosks.manage")]
    public async Task<IActionResult> SetKioskStatus(
        Guid kioskId,
        [FromBody] SetKioskStatusRequest request,
        CancellationToken cancellationToken)
    {
        EnsureValidModel();
        var context = User.GetUserContext();
        var result = await _kioskManagement.SetKioskStatusAsync(context, kioskId, request, cancellationToken);
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
