using Application.Orders.Management.Queries;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebAPI.Authorization;

namespace WebAPI.Controllers.Orders;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/management/orders/{orderId:guid}/execution-attempts")]
public sealed class ManagementExecutionAttemptsController : ControllerBase
{
    private readonly GetExecutionAttemptQueryHandler _getHandler;

    public ManagementExecutionAttemptsController(GetExecutionAttemptQueryHandler getHandler)
    {
        _getHandler = getHandler;
    }

    [HttpGet("{sourceCommandId:guid}/diagnostics")]
    [Authorize(Policy = "operations.diagnostics")]
    public async Task<IActionResult> GetExecutionAttempt(
        Guid orderId,
        Guid sourceCommandId,
        CancellationToken cancellationToken)
    {
        var result = await _getHandler.HandleAsync(new GetExecutionAttemptQuery
        {
            OrderId = orderId,
            SourceCommandId = sourceCommandId,
            UserContext = User.GetUserContext()
        }, cancellationToken);
        return StatusCode(result.StatusCode, result);
    }
}
