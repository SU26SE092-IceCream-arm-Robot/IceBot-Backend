using Application.ClientDevices.Security;
using Application.SalesCatalog.RuntimeMenus.Queries;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace WebAPI.Controllers.SalesCatalog;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/runtime/menu")]
[Authorize(AuthenticationSchemes = ClientDeviceAuthenticationDefaults.Scheme)]
public sealed class RuntimeMenusController(
    GetKioskRuntimeMenuQueryHandler queryHandler,
    ICurrentClientDeviceContext clientDevice) : ControllerBase
{
    [HttpGet]
    [EnableRateLimiting("client-device-menu")]
    public async Task<IActionResult> GetRuntimeMenu(CancellationToken cancellationToken)
    {
        var result = await queryHandler.HandleAsync(
            new GetKioskRuntimeMenuQuery(clientDevice.KioskId), cancellationToken);
        if (result.Succeeded && result.Data is not null)
        {
            var etag = $"\"{result.Data.Revision}\"";
            Response.Headers.ETag = etag;
            if (Request.Headers.IfNoneMatch.Any(value => string.Equals(value, etag, StringComparison.Ordinal)))
                return StatusCode(StatusCodes.Status304NotModified);
        }

        return StatusCode(result.StatusCode, result);
    }
}
