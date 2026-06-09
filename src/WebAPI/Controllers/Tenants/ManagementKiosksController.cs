using Application.Shared.Exceptions;
using Application.Tenants.Kiosks.Commands;
using Application.Tenants.Kiosks.Queries;
using Application.Tenants.Kiosks.Requests;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebAPI.Authorization;

namespace WebAPI.Controllers.Tenants;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/management")]
public class ManagementKiosksController : ControllerBase
{
    private readonly ListKiosksQueryHandler _listKiosks;
    private readonly GetKioskQueryHandler _getKiosk;
    private readonly CreateKioskCommandHandler _createKiosk;
    private readonly UpdateKioskCommandHandler _updateKiosk;
    private readonly SetKioskStatusCommandHandler _setKioskStatus;

    public ManagementKiosksController(
        ListKiosksQueryHandler listKiosks,
        GetKioskQueryHandler getKiosk,
        CreateKioskCommandHandler createKiosk,
        UpdateKioskCommandHandler updateKiosk,
        SetKioskStatusCommandHandler setKioskStatus)
    {
        _listKiosks = listKiosks;
        _getKiosk = getKiosk;
        _createKiosk = createKiosk;
        _updateKiosk = updateKiosk;
        _setKioskStatus = setKioskStatus;
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
        var query = new ListKiosksQuery
        {
            UserContext = context,
            OrganizationId = organizationId,
            StoreId = storeId,
            Status = status,
            Search = search
        };
        var result = await _listKiosks.HandleAsync(query, cancellationToken);
        return StatusCode(result.StatusCode, result);
    }

    [HttpGet("kiosks/{kioskId:guid}")]
    [Authorize(Policy = "kiosks.view")]
    public async Task<IActionResult> GetKiosk(
        Guid kioskId,
        CancellationToken cancellationToken)
    {
        var context = User.GetUserContext();
        var query = new GetKioskQuery
        {
            UserContext = context,
            KioskId = kioskId
        };
        var result = await _getKiosk.HandleAsync(query, cancellationToken);
        return StatusCode(result.StatusCode, result);
    }

    [HttpPost("stores/{storeId:guid}/kiosks")]
    [Authorize(Policy = "kiosks.manage")]
    public async Task<IActionResult> CreateKiosk(
        Guid storeId,
        [FromBody] CreateKioskRequest request,
        CancellationToken cancellationToken)
    {
        var context = User.GetUserContext();
        var command = new CreateKioskCommand
        {
            UserContext = context,
            StoreId = storeId,
            Request = request
        };
        var result = await _createKiosk.HandleAsync(command, cancellationToken);
        return StatusCode(result.StatusCode, result);
    }

    [HttpPut("kiosks/{kioskId:guid}")]
    [Authorize(Policy = "kiosks.update")]
    public async Task<IActionResult> UpdateKiosk(
        Guid kioskId,
        [FromBody] UpdateKioskRequest request,
        CancellationToken cancellationToken)
    {
        var context = User.GetUserContext();
        var command = new UpdateKioskCommand
        {
            UserContext = context,
            KioskId = kioskId,
            Request = request
        };
        var result = await _updateKiosk.HandleAsync(command, cancellationToken);
        return StatusCode(result.StatusCode, result);
    }

    [HttpPatch("kiosks/{kioskId:guid}/status")]
    [Authorize(Policy = "kiosks.manage")]
    public async Task<IActionResult> SetKioskStatus(
        Guid kioskId,
        [FromBody] SetKioskStatusRequest request,
        CancellationToken cancellationToken)
    {
        var context = User.GetUserContext();
        var command = new SetKioskStatusCommand
        {
            UserContext = context,
            KioskId = kioskId,
            Request = request
        };
        var result = await _setKioskStatus.HandleAsync(command, cancellationToken);
        return StatusCode(result.StatusCode, result);
    }
}
