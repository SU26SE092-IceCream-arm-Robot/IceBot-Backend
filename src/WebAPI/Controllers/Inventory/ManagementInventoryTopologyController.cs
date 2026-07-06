using Application.Inventory.Commands;
using Application.Inventory.Requests;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebAPI.Authorization;

namespace WebAPI.Controllers.Inventory;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/management")]
[Authorize(Policy = "inventory.configure")]
public sealed class ManagementInventoryTopologyController(
    CreateDispenserStateCommandHandler createHandler,
    UpdateDispenserStateCommandHandler updateHandler,
    SetDispenserStateStatusCommandHandler setStatusHandler,
    DeleteDispenserStateCommandHandler deleteHandler) : ControllerBase
{
    [HttpPost("kiosks/{kioskId:guid}/inventory/dispenser-states")]
    public async Task<IActionResult> Create(
        Guid kioskId,
        [FromBody] CreateDispenserStateRequest request,
        CancellationToken cancellationToken)
    {
        var result = await createHandler.HandleAsync(
            new CreateDispenserStateCommand(kioskId, request, User.GetUserContext()), cancellationToken);
        return StatusCode(result.StatusCode, result);
    }

    [HttpPut("inventory/dispenser-states/{dispenserStateId:guid}")]
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
    public async Task<IActionResult> SetStatus(
        Guid dispenserStateId,
        [FromBody] SetDispenserStateStatusRequest request,
        CancellationToken cancellationToken)
    {
        var result = await setStatusHandler.HandleAsync(
            new SetDispenserStateStatusCommand(dispenserStateId, request.IsActive, User.GetUserContext()), cancellationToken);
        return StatusCode(result.StatusCode, result);
    }

    [HttpDelete("inventory/dispenser-states/{dispenserStateId:guid}")]
    public async Task<IActionResult> Delete(Guid dispenserStateId, CancellationToken cancellationToken)
    {
        var result = await deleteHandler.HandleAsync(
            new DeleteDispenserStateCommand(dispenserStateId, User.GetUserContext()), cancellationToken);
        return StatusCode(result.StatusCode, result);
    }
}
