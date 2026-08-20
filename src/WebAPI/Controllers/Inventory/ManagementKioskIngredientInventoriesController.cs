using Application.Inventory.Commands;
using Application.Shared.Wrappers;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebAPI.Authorization;

namespace WebAPI.Controllers.Inventory;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/management/kiosks/{kioskId:guid}/inventory/balances")]
public sealed class ManagementKioskIngredientInventoriesController(
    CreateKioskIngredientInventoryCommandHandler createHandler,
    UpdateKioskIngredientInventoryCommandHandler updateHandler,
    AdjustKioskIngredientInventoryCommandHandler adjustHandler) : ControllerBase
{
    [HttpPost]
    [Authorize(Policy = "inventory.configure")]
    public async Task<IActionResult> Create(
        Guid kioskId,
        [FromBody] CreateKioskIngredientInventoryRequest request,
        CancellationToken cancellationToken)
    {
        var result = await createHandler.HandleAsync(
            new CreateKioskIngredientInventoryCommand(
                kioskId,
                request.IngredientId,
                request.Unit,
                request.EstimatedQuantity,
                request.LowStockThreshold,
                request.ExpiresAt,
                request.TrackingMode,
                User.GetUserContext()),
            cancellationToken);

        return ToActionResult(result);
    }

    [HttpPut("{inventoryId:guid}")]
    [Authorize(Policy = "inventory.configure")]
    public async Task<IActionResult> Update(
        Guid kioskId,
        Guid inventoryId,
        [FromBody] UpdateKioskIngredientInventoryRequest request,
        CancellationToken cancellationToken)
    {
        var result = await updateHandler.HandleAsync(
            new UpdateKioskIngredientInventoryCommand(
                kioskId,
                inventoryId,
                request.LowStockThreshold,
                request.ExpiresAt,
                request.TrackingMode,
                User.GetUserContext()),
            cancellationToken);

        return ToActionResult(result);
    }

    [HttpPost("{inventoryId:guid}/adjust-estimate")]
    [Authorize(Policy = "inventory.adjust.manage")]
    public async Task<IActionResult> AdjustEstimate(
        Guid kioskId,
        Guid inventoryId,
        [FromBody] AdjustKioskIngredientInventoryRequest request,
        CancellationToken cancellationToken)
    {
        var result = await adjustHandler.HandleAsync(
            new AdjustKioskIngredientInventoryCommand(
                kioskId,
                inventoryId,
                request.EstimatedQuantity,
                request.ReasonCode,
                User.GetUserContext()),
            cancellationToken);

        return ToActionResult(result);
    }

    private IActionResult ToActionResult<T>(ApiResult<T> result) =>
        StatusCode(result.StatusCode, result);
}
