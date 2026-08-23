using Application.ClientDevices;
using Application.ClientDevices.Contracts;
using Application.Shared.Wrappers;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace WebAPI.Controllers.Devices;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/client-device-sessions")]
public sealed class ClientDeviceSessionsController(
    ClientDeviceSessionService sessions,
    IHostEnvironment environment) : ControllerBase
{
    [HttpPost]
    [AllowAnonymous]
    [EnableRateLimiting("client-device-session")]
    [RequestSizeLimit(8_192)]
    public async Task<IActionResult> Create(
        [FromBody] CreateClientDeviceSessionRequest request,
        CancellationToken cancellationToken)
    {
        if (environment.IsProduction() && !Request.IsHttps)
            return BadRequest("Client device sessions require HTTPS.");

        if (!Guid.TryParse(Request.Headers[ClientDeviceSessionHeaderNames.ClientDeviceId], out var clientDeviceId) ||
            clientDeviceId != request.ClientDeviceId)
        {
            return BadRequest(ApiResult<object>.Fail(
                $"{ClientDeviceSessionHeaderNames.ClientDeviceId} must match clientDeviceId.",
                StatusCodes.Status400BadRequest));
        }

        var result = await sessions.CreateAsync(request, cancellationToken);
        return StatusCode(result.StatusCode, result);
    }
}
