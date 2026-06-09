using Application.Tenants.RoleScopes.Queries;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using WebAPI.Authorization;

namespace WebAPI.Controllers.Tenants;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/management/role-scope-options")]
[Authorize(Policy = "role-scope-options.view")]
public sealed class ManagementRoleScopeOptionsController : ControllerBase
{
    private readonly GetRoleScopeOptionsQueryHandler _queryHandler;

    public ManagementRoleScopeOptionsController(GetRoleScopeOptionsQueryHandler queryHandler)
    {
        _queryHandler = queryHandler;
    }

    [HttpGet]
    public async Task<IActionResult> GetRoleScopeOptions(
        [FromQuery] string roleCode,
        CancellationToken cancellationToken)
    {
        var query = new GetRoleScopeOptionsQuery
        {
            RoleCode = roleCode,
            UserContext = User.GetUserContext(),
            UserRoles = User.FindAll(ClaimTypes.Role).Select(claim => claim.Value).ToList()
        };

        var result = await _queryHandler.HandleAsync(query, cancellationToken);
        return StatusCode(result.StatusCode, result);
    }
}
