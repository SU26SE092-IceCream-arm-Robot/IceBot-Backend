using Application.Tenants.TenantTree.Services;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebAPI.Authorization;

namespace WebAPI.Controllers.Tenants;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/management/tenant-tree")]
public sealed class ManagementTenantTreeController : ControllerBase
{
    private readonly TenantTreeService _tenantTreeService;

    public ManagementTenantTreeController(TenantTreeService tenantTreeService)
    {
        _tenantTreeService = tenantTreeService;
    }

    [HttpGet]
    [Authorize(Policy = "tenant-tree.view")]
    public async Task<IActionResult> GetTenantTree(
        [FromQuery] bool includeInactive = false,
        CancellationToken cancellationToken = default)
    {
        var context = User.GetUserContext();
        var result = await _tenantTreeService.GetTenantTreeAsync(context, includeInactive, cancellationToken);
        return StatusCode(result.StatusCode, result);
    }
}
