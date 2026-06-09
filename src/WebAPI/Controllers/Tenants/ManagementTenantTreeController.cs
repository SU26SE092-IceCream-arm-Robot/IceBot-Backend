using Application.Tenants.TenantTree.Queries;
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
    private readonly GetTenantTreeQueryHandler _handler;

    public ManagementTenantTreeController(GetTenantTreeQueryHandler handler)
    {
        _handler = handler;
    }

    [HttpGet]
    [Authorize(Policy = "tenant-tree.view")]
    public async Task<IActionResult> GetTenantTree(
        [FromQuery] bool includeInactive = false,
        CancellationToken cancellationToken = default)
    {
        var context = User.GetUserContext();
        var query = new GetTenantTreeQuery
        {
            UserContext = context,
            IncludeInactive = includeInactive
        };

        var result = await _handler.HandleAsync(query, cancellationToken);
        return StatusCode(result.StatusCode, result);
    }
}
