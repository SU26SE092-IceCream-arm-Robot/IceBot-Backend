using Application.Inventory.Commands;
using Application.Inventory.Requests;
using Application.Inventory.Queries;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebAPI.Authorization;

namespace WebAPI.Controllers.Inventory;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/management")]
[Authorize]
public sealed class ManagementInventoryTopologyController(
    GetKioskInventoryTopologyQueryHandler getTopologyHandler,
    GetDispenserRebindHistoryQueryHandler getRebindHistoryHandler,
    GetDispenserHistoryQueryHandler getHistoryHandler,
    CreateDispenserStateCommandHandler createHandler,
    UpdateDispenserStateCommandHandler updateHandler,
    SetDispenserStateStatusCommandHandler setStatusHandler,
    DeleteDispenserStateCommandHandler deleteHandler,
    RebindDispenserStateCommandHandler rebindHandler) : ControllerBase
{
    [HttpGet("kiosks/{kioskId:guid}/inventory/topology")]
    [Authorize(Policy = "inventory.view")]
    public async Task<IActionResult> GetTopology(Guid kioskId, CancellationToken cancellationToken)
    {
        var result = await getTopologyHandler.HandleAsync(
            new GetKioskInventoryTopologyQuery(kioskId, User.GetUserContext()), cancellationToken);
        return StatusCode(result.StatusCode, result);
    }

    [HttpPost("kiosks/{kioskId:guid}/inventory/dispenser-states")]
    [Authorize(Policy = "inventory.configure")]
    public async Task<IActionResult> Create(
        Guid kioskId,
        [FromBody] CreateDispenserStateRequest request,
        CancellationToken cancellationToken)
    {
        var result = await createHandler.HandleAsync(
            new CreateDispenserStateCommand(kioskId, request, User.GetUserContext()), cancellationToken);
        return StatusCode(result.StatusCode, result);
    }

    [HttpPost("inventory/dispenser-states/{dispenserStateId:guid}/rebind")]
    [Authorize(Policy = "inventory.configure")]
    public async Task<IActionResult> Rebind(
        Guid dispenserStateId,
        [FromBody] RebindDispenserStateRequest request,
        CancellationToken cancellationToken)
    {
        var result = await rebindHandler.HandleAsync(
            new RebindDispenserStateCommand(dispenserStateId, request, User.GetUserContext()), cancellationToken);
        return StatusCode(result.StatusCode, result);
    }

    [HttpGet("inventory/dispenser-states/{dispenserStateId:guid}/rebind-history")]
    [Authorize(Policy = "inventory.view")]
    public async Task<IActionResult> GetRebindHistory(Guid dispenserStateId, CancellationToken cancellationToken)
    {
        var result = await getRebindHistoryHandler.HandleAsync(
            new GetDispenserRebindHistoryQuery(dispenserStateId, User.GetUserContext()), cancellationToken);
        return StatusCode(result.StatusCode, result);
    }

    [HttpGet("inventory/dispenser-states/{dispenserStateId:guid}/history")]
    [Authorize(Policy = "inventory.view")]
    public async Task<IActionResult> GetHistory(
        Guid dispenserStateId,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await getHistoryHandler.HandleAsync(
            new GetDispenserHistoryQuery(
                dispenserStateId,
                pageNumber,
                pageSize,
                User.GetUserContext()),
            cancellationToken);
        return StatusCode(result.StatusCode, result);
    }

    [HttpPut("inventory/dispenser-states/{dispenserStateId:guid}")]
    [Authorize(Policy = "inventory.configure")]
    public async Task<IActionResult> Update(
        Guid dispenserStateId,
        [FromBody] UpdateDispenserStateRequest request,
        CancellationToken cancellationToken)
    {
        var result = await updateHandler.HandleAsync(
            new UpdateDispenserStateCommand(dispenserStateId, request, User.GetUserContext()), cancellationToken);
        return StatusCode(result.StatusCode, result);
    }

    [HttpPatch("inventory/dispenser-states/{dispenserStateId:guid}/status")]
    [Authorize(Policy = "inventory.configure")]
    public async Task<IActionResult> SetStatus(
        Guid dispenserStateId,
        [FromBody] SetDispenserStateStatusRequest request,
        CancellationToken cancellationToken)
    {
        var result = await setStatusHandler.HandleAsync(
            new SetDispenserStateStatusCommand(dispenserStateId, request.IsActive, request.Reason, User.GetUserContext()), cancellationToken);
        return StatusCode(result.StatusCode, result);
    }

    [HttpDelete("inventory/dispenser-states/{dispenserStateId:guid}")]
    [Authorize(Policy = "inventory.configure")]
    public async Task<IActionResult> Delete(Guid dispenserStateId, CancellationToken cancellationToken)
    {
        var result = await deleteHandler.HandleAsync(
            new DeleteDispenserStateCommand(dispenserStateId, User.GetUserContext()), cancellationToken);
        return StatusCode(result.StatusCode, result);
    }
}
