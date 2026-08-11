using Application.Identity.Roles.Queries;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using WebAPI.Authorization;

namespace WebAPI.Controllers.Identity;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/management")]
public sealed class ManagementRolesController : ControllerBase
{
    private readonly ListManagementRolesQueryHandler _listRolesHandler;
    public ManagementRolesController(ListManagementRolesQueryHandler listRolesHandler)
    {
        _listRolesHandler = listRolesHandler;
    }

    [HttpGet("accounts/assignable-role-options")]
    [Authorize(Policy = "accounts.manage")]
    public async Task<IActionResult> ListAssignableRoleOptions(CancellationToken cancellationToken)
    {
        var userContext = User.GetUserContext();
        var userRoles = User.FindAll(ClaimTypes.Role).Select(c => c.Value).ToList();

        var query = new ListManagementRolesQuery
        {
            UserContext = userContext,
            UserRoles = userRoles
        };

        var result = await _listRolesHandler.HandleAsync(query, cancellationToken);
        return StatusCode(result.StatusCode, result);
    }

}
