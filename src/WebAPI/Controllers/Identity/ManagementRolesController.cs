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
    private readonly GetPermissionMatrixQueryHandler _permissionMatrixHandler;

    public ManagementRolesController(
        ListManagementRolesQueryHandler listRolesHandler,
        GetPermissionMatrixQueryHandler permissionMatrixHandler)
    {
        _listRolesHandler = listRolesHandler;
        _permissionMatrixHandler = permissionMatrixHandler;
    }

    [HttpGet("roles")]
    [Authorize(Policy = "roles.view")]
    public async Task<IActionResult> ListRoles(CancellationToken cancellationToken)
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

    [HttpGet("permission-matrix")]
    [Authorize(Policy = "roles.view")]
    public async Task<IActionResult> GetPermissionMatrix(CancellationToken cancellationToken)
    {
        var query = new GetPermissionMatrixQuery();
        var result = await _permissionMatrixHandler.HandleAsync(query, cancellationToken);
        return StatusCode(result.StatusCode, result);
    }
}
