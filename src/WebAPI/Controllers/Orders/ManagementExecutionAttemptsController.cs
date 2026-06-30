using Application.Orders.Management.Queries;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebAPI.Authorization;

namespace WebAPI.Controllers.Orders;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/management/execution-attempts")]
public sealed class ManagementExecutionAttemptsController : ControllerBase
{
    private readonly GetExecutionAttemptQueryHandler _getHandler;

    public ManagementExecutionAttemptsController(GetExecutionAttemptQueryHandler getHandler)
    {
        _getHandler = getHandler;
    }

    [HttpGet("{sourceCommandId:guid}")]
    [Authorize(Policy = "orders.view")]
    public async Task<IActionResult> GetExecutionAttempt(
        Guid sourceCommandId,
        CancellationToken cancellationToken)
    {
        var result = await _getHandler.HandleAsync(new GetExecutionAttemptQuery
        {
            SourceCommandId = sourceCommandId,
            UserContext = User.GetUserContext()
        }, cancellationToken);
        return StatusCode(result.StatusCode, result);
    }
}
