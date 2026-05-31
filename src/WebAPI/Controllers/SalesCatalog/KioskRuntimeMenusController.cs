using Application.SalesCatalog.RuntimeMenus.Services;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WebAPI.Controllers.SalesCatalog;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/kiosks/{kioskId:guid}/runtime-menu")]
public sealed class KioskRuntimeMenusController : ControllerBase
{
    private readonly RuntimeMenuService _runtimeMenus;

    public KioskRuntimeMenusController(RuntimeMenuService runtimeMenus)
    {
        _runtimeMenus = runtimeMenus;
    }

    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> GetRuntimeMenu(
        Guid kioskId,
        CancellationToken cancellationToken)
    {
        var result = await _runtimeMenus.GetKioskRuntimeMenuAsync(kioskId, cancellationToken);
        return StatusCode(result.StatusCode, result);
    }
}
