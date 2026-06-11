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
[Route("api/v{version:apiVersion}/management/inventory")]
public sealed class ManagementInventoryController : ControllerBase
{
    private readonly GetDispenserStatesQueryHandler _getStatesHandler;
    private readonly GetStockMovementsQueryHandler _getMovementsHandler;
    private readonly RefillDispenserCommandHandler _refillHandler;
    private readonly AdjustDispenserEstimateCommandHandler _adjustHandler;

    public ManagementInventoryController(
        GetDispenserStatesQueryHandler getStatesHandler,
        GetStockMovementsQueryHandler getMovementsHandler,
        RefillDispenserCommandHandler refillHandler,
        AdjustDispenserEstimateCommandHandler adjustHandler)
    {
        _getStatesHandler = getStatesHandler;
        _getMovementsHandler = getMovementsHandler;
        _refillHandler = refillHandler;
        _adjustHandler = adjustHandler;
    }

    [HttpGet("dispenser-states")]
    [Authorize(Policy = "inventory.view")]
    public async Task<IActionResult> GetDispenserStates(
        [FromQuery] Guid? organizationId,
        [FromQuery] Guid? storeId,
        [FromQuery] Guid? kioskId,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var query = new GetDispenserStatesQuery
        {
            UserContext = User.GetUserContext(),
            OrganizationId = organizationId,
            StoreId = storeId,
            KioskId = kioskId,
            PageNumber = pageNumber,
            PageSize = pageSize
        };

        var result = await _getStatesHandler.HandleAsync(query, cancellationToken);
        return StatusCode(result.StatusCode, result);
    }

    [HttpGet("stock-movements")]
    [Authorize(Policy = "inventory.view")]
    public async Task<IActionResult> GetStockMovements(
        [FromQuery] Guid? organizationId,
        [FromQuery] Guid? storeId,
        [FromQuery] Guid? kioskId,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var query = new GetStockMovementsQuery
        {
            UserContext = User.GetUserContext(),
            OrganizationId = organizationId,
            StoreId = storeId,
            KioskId = kioskId,
            PageNumber = pageNumber,
            PageSize = pageSize
        };

        var result = await _getMovementsHandler.HandleAsync(query, cancellationToken);
        return StatusCode(result.StatusCode, result);
    }

    [HttpPost("dispenser-states/{id:guid}/refill")]
    [Authorize(Policy = "inventory.manage")]
    public async Task<IActionResult> RefillDispenser(
        Guid id,
        [FromBody] RefillDispenserRequest request,
        CancellationToken cancellationToken)
    {
        if (request is null)
        {
            return BadRequest(ApiResult<object>.Fail("Request body is required.", 400));
        }

        var command = new RefillDispenserCommand
        {
            DispenserStateId = id,
            UserContext = User.GetUserContext(),
            Quantity = request.Quantity,
            ReportedLevelAfter = request.ReportedLevelAfter,
            ReasonCode = request.ReasonCode
        };

        var result = await _refillHandler.HandleAsync(command, cancellationToken);
        return StatusCode(result.StatusCode, result);
    }

    [HttpPost("dispenser-states/{id:guid}/adjust-estimate")]
    [Authorize(Policy = "inventory.manage")]
    public async Task<IActionResult> AdjustDispenserEstimate(
        Guid id,
        [FromBody] AdjustDispenserEstimateRequest request,
        CancellationToken cancellationToken)
    {
        if (request is null)
        {
            return BadRequest(ApiResult<object>.Fail("Request body is required.", 400));
        }

        var command = new AdjustDispenserEstimateCommand
        {
            DispenserStateId = id,
            UserContext = User.GetUserContext(),
            EstimatedQuantity = request.EstimatedQuantity,
            ReportedLevelAfter = request.ReportedLevelAfter,
            ReasonCode = request.ReasonCode
        };

        var result = await _adjustHandler.HandleAsync(command, cancellationToken);
        return StatusCode(result.StatusCode, result);
    }
}

public sealed class RefillDispenserRequest
{
    public decimal Quantity { get; init; }
    public IngredientLevelStatus? ReportedLevelAfter { get; init; }
    public string? ReasonCode { get; init; }
}

public sealed class AdjustDispenserEstimateRequest
{
    public decimal EstimatedQuantity { get; init; }
    public IngredientLevelStatus? ReportedLevelAfter { get; init; }
    public string? ReasonCode { get; init; }
}
