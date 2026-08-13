using Application.SalesCatalog.Availability;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebAPI.Authorization;

namespace WebAPI.Controllers.SalesCatalog;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/management/kiosks/{kioskId:guid}")]
public sealed class ManagementKioskMenuItemAvailabilityController(
    ListKioskMenuItemAvailabilityQueryHandler listHandler,
    SetKioskMenuItemAvailabilityCommandHandler setHandler) : ControllerBase
{
    [HttpGet("menu-item-availability")]
    [Authorize(Policy = "menu-items.availability.manage")]
    public async Task<IActionResult> List(
        Guid kioskId,
        [FromQuery] string? search,
        [FromQuery] Domain.SalesCatalog.Enums.MenuItemOperationalAvailabilityState? state,
        CancellationToken cancellationToken)
    {
        var result = await listHandler.HandleAsync(new ListKioskMenuItemAvailabilityQuery
        {
            UserContext = User.GetUserContext(),
            KioskId = kioskId,
            Search = search,
            State = state
        }, cancellationToken);
        return StatusCode(result.StatusCode, result);
    }

    [HttpPut("menu-items/{menuItemId:guid}/availability")]
    [Authorize(Policy = "menu-items.availability.manage")]
    public async Task<IActionResult> Set(
        Guid kioskId,
        Guid menuItemId,
        [FromBody] SetKioskMenuItemAvailabilityRequest request,
        CancellationToken cancellationToken)
    {
        var result = await setHandler.HandleAsync(new SetKioskMenuItemAvailabilityCommand
        {
            UserContext = User.GetUserContext(),
            KioskId = kioskId,
            MenuItemId = menuItemId,
            Request = request
        }, cancellationToken);
        return StatusCode(result.StatusCode, result);
    }
}
