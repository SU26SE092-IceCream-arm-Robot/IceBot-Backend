using Application.Identity.Roles.Queries;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WebAPI.Controllers.Identity;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/management")]
public sealed class ManagementAuthorizationController : ControllerBase
{
    private readonly GetPermissionMatrixQueryHandler _permissionMatrixHandler;

    public ManagementAuthorizationController(GetPermissionMatrixQueryHandler permissionMatrixHandler)
    {
        _permissionMatrixHandler = permissionMatrixHandler;
    }

    [HttpGet("permission-matrix")]
    [Authorize(Policy = "permission-matrix.view")]
    public async Task<IActionResult> GetPermissionMatrix(CancellationToken cancellationToken)
    {
        var result = await _permissionMatrixHandler.HandleAsync(
            new GetPermissionMatrixQuery(), cancellationToken);
        return StatusCode(result.StatusCode, result);
    }
}
