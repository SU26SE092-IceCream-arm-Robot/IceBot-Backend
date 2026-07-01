using System.ComponentModel.DataAnnotations;
using Application.Shared.Wrappers;
using Application.Sync;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebAPI.Authorization;

namespace WebAPI.Controllers.Operations;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/management/sync-dead-letters")]
[Authorize(Policy = "sync-dead-letters.manage")]
public sealed class ManagementSyncDeadLettersController : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List([FromServices] ListSyncDeadLettersQueryHandler handler,
        [FromQuery] string? status, [FromQuery] string? eventType, [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20, CancellationToken cancellationToken = default)
    {
        var result = await handler.HandleAsync(new(User.GetUserContext(), status, eventType, pageNumber, pageSize), cancellationToken);
        return StatusCode(result.StatusCode, result);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id, [FromServices] GetSyncDeadLetterQueryHandler handler, CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new(User.GetUserContext(), id), cancellationToken);
        return StatusCode(result.StatusCode, result);
    }

    [HttpPost("{id:guid}/retry")]
    public async Task<IActionResult> Retry(Guid id, [FromBody] DeadLetterActionRequest request,
        [FromServices] RetrySyncDeadLetterCommandHandler handler, CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new(User.GetUserContext(), id, request.Reason), cancellationToken);
        return StatusCode(result.StatusCode, result);
    }

    [HttpPost("{id:guid}/resolve")]
    public async Task<IActionResult> Resolve(Guid id, [FromBody] DeadLetterActionRequest request,
        [FromServices] ResolveSyncDeadLetterCommandHandler handler, CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new(User.GetUserContext(), id, request.Reason), cancellationToken);
        return StatusCode(result.StatusCode, result);
    }

    [HttpPost("{id:guid}/ignore")]
    public async Task<IActionResult> Ignore(Guid id, [FromBody] DeadLetterActionRequest request,
        [FromServices] IgnoreSyncDeadLetterCommandHandler handler, CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new(User.GetUserContext(), id, request.Reason), cancellationToken);
        return StatusCode(result.StatusCode, result);
    }
}

public sealed class DeadLetterActionRequest
{
    [Required, StringLength(1000)] public string Reason { get; init; } = string.Empty;
}
