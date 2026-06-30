using Application.Operations.Alerts.Commands;
using Application.Operations.Alerts.Queries;
using Application.Operations.Alerts.Requests;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebAPI.Authorization;

namespace WebAPI.Controllers.Operations;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/management/alerts")]
public sealed class ManagementAlertsController : ControllerBase
{
    private readonly ListAlertsQueryHandler _listHandler;
    private readonly GetAlertQueryHandler _getHandler;
    private readonly AcknowledgeAlertCommandHandler _acknowledgeHandler;
    private readonly ResolveAlertCommandHandler _resolveHandler;

    public ManagementAlertsController(
        ListAlertsQueryHandler listHandler,
        GetAlertQueryHandler getHandler,
        AcknowledgeAlertCommandHandler acknowledgeHandler,
        ResolveAlertCommandHandler resolveHandler)
    {
        _listHandler = listHandler;
        _getHandler = getHandler;
        _acknowledgeHandler = acknowledgeHandler;
        _resolveHandler = resolveHandler;
    }

    [HttpGet]
    [Authorize(Policy = "alerts.view")]
    public async Task<IActionResult> List(
        [FromQuery] string? status,
        [FromQuery] string? severity,
        [FromQuery] Guid? organizationId,
        [FromQuery] Guid? storeId,
        [FromQuery] Guid? kioskId,
        [FromQuery] Guid? deviceId,
        [FromQuery] DateTimeOffset? from,
        [FromQuery] DateTimeOffset? to,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await _listHandler.HandleAsync(new ListAlertsQuery
        {
            UserContext = User.GetUserContext(),
            Status = status,
            Severity = severity,
            OrganizationId = organizationId,
            StoreId = storeId,
            KioskId = kioskId,
            DeviceId = deviceId,
            From = from,
            To = to,
            PageNumber = pageNumber,
            PageSize = pageSize
        }, cancellationToken);
        return StatusCode(result.StatusCode, result);
    }

    [HttpGet("{alertId:guid}")]
    [Authorize(Policy = "alerts.view")]
    public async Task<IActionResult> Get(Guid alertId, CancellationToken cancellationToken = default)
    {
        var result = await _getHandler.HandleAsync(new GetAlertQuery
        {
            AlertId = alertId,
            UserContext = User.GetUserContext()
        }, cancellationToken);
        return StatusCode(result.StatusCode, result);
    }

    [HttpPatch("{alertId:guid}/acknowledge")]
    [Authorize(Policy = "alerts.manage")]
    public async Task<IActionResult> Acknowledge(Guid alertId, CancellationToken cancellationToken = default)
    {
        var result = await _acknowledgeHandler.HandleAsync(new AcknowledgeAlertCommand
        {
            AlertId = alertId,
            UserContext = User.GetUserContext()
        }, cancellationToken);
        return StatusCode(result.StatusCode, result);
    }

    [HttpPatch("{alertId:guid}/resolve")]
    [Authorize(Policy = "alerts.manage")]
    public async Task<IActionResult> Resolve(
        Guid alertId,
        [FromBody] ResolveAlertRequest request,
        CancellationToken cancellationToken = default)
    {
        var result = await _resolveHandler.HandleAsync(new ResolveAlertCommand
        {
            AlertId = alertId,
            UserContext = User.GetUserContext(),
            Request = request
        }, cancellationToken);
        return StatusCode(result.StatusCode, result);
    }
}
