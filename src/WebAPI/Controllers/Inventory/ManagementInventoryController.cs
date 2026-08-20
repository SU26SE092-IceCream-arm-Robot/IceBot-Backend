using Application.Inventory.Queries;
using Application.Shared.Wrappers;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebAPI.Authorization;

namespace WebAPI.Controllers.Inventory;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/management")]
public sealed class ManagementInventoryController(
    GetDispenserStatesQueryHandler getStatesHandler,
    GetStockMovementsQueryHandler getMovementsHandler) : ControllerBase
{
    [HttpGet("inventory/dispenser-states")]
    [Authorize(Policy = "inventory.view")]
    public async Task<IActionResult> GetDispenserStates(
        [FromQuery] Guid? organizationId,
        [FromQuery] Guid? storeId,
        [FromQuery] Guid? kioskId,
        [FromQuery] bool? isActive,
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
            IsActive = isActive,
            PageNumber = pageNumber,
            PageSize = pageSize
        };

        var result = await getStatesHandler.HandleAsync(query, cancellationToken);
        return StatusCode(result.StatusCode, result);
    }

    [HttpGet("inventory/stock-movements")]
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

        var result = await getMovementsHandler.HandleAsync(query, cancellationToken);
        return StatusCode(result.StatusCode, result);
    }
}
