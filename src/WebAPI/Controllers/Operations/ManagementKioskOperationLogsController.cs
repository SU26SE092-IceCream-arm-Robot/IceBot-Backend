using Application.Operations.OperationLogs.Queries;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebAPI.Authorization;

namespace WebAPI.Controllers.Operations;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/management/kiosks/{kioskId:guid}/operation-logs")]
public sealed class ManagementKioskOperationLogsController : ControllerBase
{
    private readonly ListOperationLogsQueryHandler _listHandler;
    private readonly GetOperationLogQueryHandler _getHandler;
    private readonly GetOperationLogDiagnosticsQueryHandler _diagnosticsHandler;

    public ManagementKioskOperationLogsController(
        ListOperationLogsQueryHandler listHandler,
        GetOperationLogQueryHandler getHandler,
        GetOperationLogDiagnosticsQueryHandler diagnosticsHandler)
    {
        _listHandler = listHandler;
        _getHandler = getHandler;
        _diagnosticsHandler = diagnosticsHandler;
    }

    [HttpGet]
    [Authorize(Policy = "operations.view")]
    public async Task<IActionResult> List(
        Guid kioskId,
        [FromQuery] Guid? deviceId,
        [FromQuery] Guid? orderId,
        [FromQuery] string? severity,
        [FromQuery] DateTimeOffset? from,
        [FromQuery] DateTimeOffset? to,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await _listHandler.HandleAsync(new ListOperationLogsQuery
        {
            UserContext = User.GetUserContext(),
            KioskId = kioskId,
            DeviceId = deviceId,
            OrderId = orderId,
            Severity = severity,
            From = from,
            To = to,
            PageNumber = pageNumber,
            PageSize = pageSize
        }, cancellationToken);
        return StatusCode(result.StatusCode, result);
    }

    [HttpGet("{operationLogId:guid}")]
    [Authorize(Policy = "operations.view")]
    public async Task<IActionResult> Get(
        Guid kioskId,
        Guid operationLogId,
        CancellationToken cancellationToken = default)
    {
        var result = await _getHandler.HandleAsync(CreateGetQuery(kioskId, operationLogId), cancellationToken);
        return StatusCode(result.StatusCode, result);
    }

    [HttpGet("{operationLogId:guid}/diagnostics")]
    [Authorize(Policy = "operations.diagnostics")]
    public async Task<IActionResult> GetDiagnostics(
        Guid kioskId,
        Guid operationLogId,
        CancellationToken cancellationToken = default)
    {
        var result = await _diagnosticsHandler.HandleAsync(CreateGetQuery(kioskId, operationLogId), cancellationToken);
        return StatusCode(result.StatusCode, result);
    }

    private GetOperationLogQuery CreateGetQuery(Guid kioskId, Guid operationLogId) => new()
    {
        UserContext = User.GetUserContext(),
        KioskId = kioskId,
        OperationLogId = operationLogId
    };
}
