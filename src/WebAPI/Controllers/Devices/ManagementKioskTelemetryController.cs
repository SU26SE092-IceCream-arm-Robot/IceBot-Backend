using Application.Devices.Queries;
using Asp.Versioning;
using Domain.Common.Enums;
using Domain.Devices.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebAPI.Authorization;

namespace WebAPI.Controllers.Devices;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/management/kiosks/{kioskId:guid}")]
[Authorize(Policy = "operations.view")]
public sealed class ManagementKioskTelemetryController : ControllerBase
{
    private readonly GetKioskHeartbeatsQueryHandler _heartbeatsHandler;
    private readonly GetKioskDeviceEventsQueryHandler _eventsHandler;

    public ManagementKioskTelemetryController(
        GetKioskHeartbeatsQueryHandler heartbeatsHandler,
        GetKioskDeviceEventsQueryHandler eventsHandler)
    {
        _heartbeatsHandler = heartbeatsHandler;
        _eventsHandler = eventsHandler;
    }

    [HttpGet("heartbeats")]
    public async Task<IActionResult> GetHeartbeats(
        Guid kioskId,
        [FromQuery] KioskHeartbeatStatus? status,
        [FromQuery] DateTimeOffset? from,
        [FromQuery] DateTimeOffset? to,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var query = new GetKioskHeartbeatsQuery
        {
            UserContext = User.GetUserContext(),
            KioskId = kioskId,
            Status = status,
            From = from,
            To = to,
            PageNumber = pageNumber,
            PageSize = pageSize
        };

        var result = await _heartbeatsHandler.HandleAsync(query, cancellationToken);
        return StatusCode(result.StatusCode, result);
    }

    [HttpGet("device-events")]
    public async Task<IActionResult> GetEvents(
        Guid kioskId,
        [FromQuery] SeverityLevel? minSeverity,
        [FromQuery] string? eventType,
        [FromQuery] DateTimeOffset? from,
        [FromQuery] DateTimeOffset? to,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var query = new GetKioskDeviceEventsQuery
        {
            UserContext = User.GetUserContext(),
            KioskId = kioskId,
            MinSeverity = minSeverity,
            EventType = eventType,
            From = from,
            To = to,
            PageNumber = pageNumber,
            PageSize = pageSize
        };

        var result = await _eventsHandler.HandleAsync(query, cancellationToken);
        return StatusCode(result.StatusCode, result);
    }
}
