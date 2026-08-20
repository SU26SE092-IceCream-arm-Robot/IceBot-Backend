using Application.Inventory.Queries;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebAPI.Authorization;

namespace WebAPI.Controllers.Inventory;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/management/kiosks/{kioskId:guid}/inventory/workspace")]
[Authorize(Policy = "inventory.view")]
public sealed class ManagementKioskInventoryWorkspaceController(GetKioskInventoryWorkspaceQueryHandler handler) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get(Guid kioskId, CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(
            new GetKioskInventoryWorkspaceQuery(kioskId, User.GetUserContext()),
            cancellationToken);
        return StatusCode(result.StatusCode, result);
    }
}
