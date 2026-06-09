using Application.SalesCatalog.RuntimeMenus.Queries;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WebAPI.Controllers.SalesCatalog;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/kiosks/{kioskId:guid}/runtime-menu")]
public sealed class KioskRuntimeMenusController : ControllerBase
{
    private readonly GetKioskRuntimeMenuQueryHandler _queryHandler;

    public KioskRuntimeMenusController(GetKioskRuntimeMenuQueryHandler queryHandler)
    {
        _queryHandler = queryHandler;
    }

    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> GetRuntimeMenu(
        Guid kioskId,
        CancellationToken cancellationToken)
    {
        var query = new GetKioskRuntimeMenuQuery(kioskId);
        var result = await _queryHandler.HandleAsync(query, cancellationToken);
        return StatusCode(result.StatusCode, result);
    }
}
