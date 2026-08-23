using Application.ClientDevices;
using Application.ClientDevices.Contracts;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebAPI.Authorization;

namespace WebAPI.Controllers.Devices;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/management")]
[Authorize]
public sealed class ManagementClientDevicesController(ClientDeviceManagementService devices) : ControllerBase
{
    [HttpGet("kiosks/{kioskId:guid}/client-devices")]
    [Authorize(Policy = "client-devices.view")]
    public async Task<IActionResult> List(Guid kioskId, CancellationToken cancellationToken) =>
        ToActionResult(await devices.ListAsync(kioskId, User.GetUserContext(), cancellationToken));

    [HttpGet("client-devices/{clientDeviceId:guid}")]
    [Authorize(Policy = "client-devices.view")]
    public async Task<IActionResult> Get(Guid clientDeviceId, CancellationToken cancellationToken) =>
        ToActionResult(await devices.GetAsync(clientDeviceId, User.GetUserContext(), cancellationToken));

    [HttpPost("kiosks/{kioskId:guid}/client-devices")]
    [Authorize(Policy = "client-devices.provision")]
    public async Task<IActionResult> Provision(
        Guid kioskId,
        [FromBody] ProvisionClientDeviceRequest request,
        [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey,
        CancellationToken cancellationToken) =>
        ToActionResult(await devices.ProvisionAsync(kioskId, request, idempotencyKey, User.GetUserContext(), cancellationToken));

    [HttpPost("client-devices/{clientDeviceId:guid}/disable")]
    [Authorize(Policy = "client-devices.operations.manage")]
    public async Task<IActionResult> Disable(Guid clientDeviceId, [FromBody] ClientDeviceLifecycleRequest request,
        [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey, CancellationToken cancellationToken) =>
        ToActionResult(await devices.DisableAsync(clientDeviceId, request, idempotencyKey, User.GetUserContext(), cancellationToken));

    [HttpPost("client-devices/{clientDeviceId:guid}/reactivate")]
    [Authorize(Policy = "client-devices.operations.manage")]
    public async Task<IActionResult> Reactivate(Guid clientDeviceId, [FromBody] ClientDeviceLifecycleRequest request,
        [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey, CancellationToken cancellationToken) =>
        ToActionResult(await devices.ReactivateAsync(clientDeviceId, request, idempotencyKey, User.GetUserContext(), cancellationToken));

    [HttpPost("client-devices/{clientDeviceId:guid}/rotate-credential")]
    [Authorize(Policy = "client-devices.credentials.manage")]
    public async Task<IActionResult> RotateCredential(Guid clientDeviceId, [FromBody] RotateClientDeviceCredentialRequest request,
        [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey, CancellationToken cancellationToken) =>
        ToActionResult(await devices.RotateCredentialAsync(clientDeviceId, request, idempotencyKey, User.GetUserContext(), cancellationToken));

    [HttpPost("client-devices/{clientDeviceId:guid}/rebind")]
    [Authorize(Policy = "client-devices.rebind")]
    public async Task<IActionResult> Rebind(Guid clientDeviceId, [FromBody] RebindClientDeviceRequest request,
        [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey, CancellationToken cancellationToken) =>
        ToActionResult(await devices.RebindAsync(clientDeviceId, request, idempotencyKey, User.GetUserContext(), cancellationToken));

    [HttpPost("client-devices/{clientDeviceId:guid}/retire")]
    [Authorize(Policy = "client-devices.rebind")]
    public async Task<IActionResult> Retire(Guid clientDeviceId, [FromBody] ClientDeviceLifecycleRequest request,
        [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey, CancellationToken cancellationToken) =>
        ToActionResult(await devices.RetireAsync(clientDeviceId, request, idempotencyKey, User.GetUserContext(), cancellationToken));

    [HttpPost("kiosks/{kioskId:guid}/client-devices/replace")]
    [Authorize(Policy = "client-devices.rebind")]
    public async Task<IActionResult> Replace(Guid kioskId, [FromBody] ReplaceClientDeviceRequest request,
        [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey, CancellationToken cancellationToken) =>
        ToActionResult(await devices.ReplaceAsync(kioskId, request, idempotencyKey, User.GetUserContext(), cancellationToken));

    private IActionResult ToActionResult<T>(Application.Shared.Wrappers.ApiResult<T> result) =>
        StatusCode(result.StatusCode, result);
}
