using Application.Inventory.Commands;
using Application.Inventory.Queries;
using Application.Shared.Wrappers;
using Asp.Versioning;
using Domain.Inventory.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebAPI.Authorization;

namespace WebAPI.Controllers.Inventory;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/management/kiosks/{kioskId:guid}/inventory")]
public sealed class ManagementInventoryRefillTasksController(
    ListInventoryRefillTasksQueryHandler listHandler,
    GetInventoryRefillTaskQueryHandler getHandler,
    RequestInventoryRefillTaskCommandHandler requestHandler,
    StartInventoryRefillTaskCommandHandler startHandler,
    CompleteInventoryRefillTaskCommandHandler completeHandler,
    CancelInventoryRefillTaskCommandHandler cancelHandler) : ControllerBase
{
    [HttpGet("refill-tasks")]
    [Authorize(Policy = "inventory.view")]
    public async Task<IActionResult> List(
        Guid kioskId,
        [FromQuery] InventoryRefillTaskStatus? status,
        [FromQuery] DateTimeOffset? requestedFrom,
        [FromQuery] DateTimeOffset? requestedTo,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await listHandler.HandleAsync(
            new ListInventoryRefillTasksQuery(
                kioskId,
                status,
                requestedFrom,
                requestedTo,
                pageNumber,
                pageSize,
                User.GetUserContext()),
            cancellationToken);

        return ToActionResult(result);
    }

    [HttpGet("refill-tasks/{taskId:guid}")]
    [Authorize(Policy = "inventory.view")]
    public async Task<IActionResult> Get(
        Guid kioskId,
        Guid taskId,
        CancellationToken cancellationToken)
    {
        var result = await getHandler.HandleAsync(
            new GetInventoryRefillTaskQuery(kioskId, taskId, User.GetUserContext()),
            cancellationToken);

        return ToActionResult(result);
    }

    [HttpPost("balances/{inventoryId:guid}/refill-tasks")]
    [Authorize(Policy = "inventory.refill.manage")]
    public async Task<IActionResult> Request(
        Guid kioskId,
        Guid inventoryId,
        [FromBody] RequestInventoryRefillTaskRequest request,
        [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey,
        CancellationToken cancellationToken)
    {
        var result = await requestHandler.HandleAsync(
            new RequestInventoryRefillTaskCommand(
                kioskId,
                inventoryId,
                request.RequestedQuantity,
                request.IngredientDispenserStateId,
                request.ReasonCode,
                request.Notes,
                idempotencyKey ?? string.Empty,
                User.GetUserContext()),
            cancellationToken);

        return ToActionResult(result);
    }

    [HttpPost("refill-tasks/{taskId:guid}/start")]
    [Authorize(Policy = "inventory.refill.manage")]
    public async Task<IActionResult> Start(
        Guid kioskId,
        Guid taskId,
        [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey,
        CancellationToken cancellationToken)
    {
        var result = await startHandler.HandleAsync(
            new StartInventoryRefillTaskCommand(
                kioskId,
                taskId,
                idempotencyKey ?? string.Empty,
                User.GetUserContext()),
            cancellationToken);

        return ToActionResult(result);
    }

    [HttpPost("refill-tasks/{taskId:guid}/complete")]
    [Authorize(Policy = "inventory.refill.manage")]
    public async Task<IActionResult> Complete(
        Guid kioskId,
        Guid taskId,
        [FromBody] CompleteInventoryRefillTaskRequest request,
        [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey,
        CancellationToken cancellationToken)
    {
        var result = await completeHandler.HandleAsync(
            new CompleteInventoryRefillTaskCommand(
                kioskId,
                taskId,
                request.ActualQuantity,
                request.IngredientDispenserStateId,
                request.ReasonCode,
                request.Notes,
                request.ExternalLotReference,
                idempotencyKey ?? string.Empty,
                User.GetUserContext()),
            cancellationToken);

        return ToActionResult(result);
    }

    [HttpPost("refill-tasks/{taskId:guid}/cancel")]
    [Authorize(Policy = "inventory.refill.manage")]
    public async Task<IActionResult> Cancel(
        Guid kioskId,
        Guid taskId,
        [FromBody] CancelInventoryRefillTaskRequest request,
        [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey,
        CancellationToken cancellationToken)
    {
        var result = await cancelHandler.HandleAsync(
            new CancelInventoryRefillTaskCommand(
                kioskId,
                taskId,
                request.Reason,
                idempotencyKey ?? string.Empty,
                User.GetUserContext()),
            cancellationToken);

        return ToActionResult(result);
    }

    private IActionResult ToActionResult<T>(ApiResult<T> result) =>
        StatusCode(result.StatusCode, result);
}
