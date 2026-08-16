using System.ComponentModel.DataAnnotations;
using Application.Devices.Connectivity.Commands;
using Application.Shared.Wrappers;
using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using WebAPI.Configuration.Security;

namespace WebAPI.Controllers.IoT;

[ApiController, ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/iot/execution-endpoints/{endpointId:guid}/reported-devices")]
public sealed class ExecutionReportedDevicesController : ControllerBase
{
    private readonly ReplaceExecutionEndpointReportedDevicesCommandHandler _handler;
    private readonly ExecutionEndpointRequestAuthenticator _authenticator;

    public ExecutionReportedDevicesController(
        ReplaceExecutionEndpointReportedDevicesCommandHandler handler,
        ExecutionEndpointRequestAuthenticator authenticator)
    {
        _handler = handler;
        _authenticator = authenticator;
    }

    [HttpPut]
    public async Task<IActionResult> Replace(
        Guid endpointId,
        [FromBody] ExecutionReportedDevicesRequest request,
        CancellationToken ct)
    {
        var auth = await _authenticator.AuthenticateAsync(HttpContext, endpointId, ct);
        if (!auth.Succeeded) return Unauthorized(ApiResult<object>.Fail(auth.Message, 401));
        var result = await _handler.HandleAsync(new ReplaceExecutionEndpointReportedDevicesCommand
        {
            KioskId = auth.Endpoint!.KioskId,
            EndpointId = endpointId,
            SourceExecutorId = request.SourceExecutorId,
            SnapshotRevision = request.SnapshotRevision,
            ObservedAt = request.ObservedAt,
            Devices = request.Devices.Select(item => new Domain.Devices.ExecutionEndpoints.ReportedDeviceSnapshotItem(
                item.SourceDeviceKey,
                item.DeviceId,
                item.RuntimeTargetCode,
                item.MachineModelCode)).ToArray()
        }, ct);
        return StatusCode(result.StatusCode, result);
    }
}

public sealed class ExecutionReportedDevicesRequest
{
    [Required] public Guid SourceExecutorId { get; init; }
    [Range(1, long.MaxValue)] public long SnapshotRevision { get; init; }
    [Required] public DateTimeOffset ObservedAt { get; init; }
    [MaxLength(50)] public IReadOnlyList<ExecutionReportedDeviceRequest> Devices { get; init; } = [];
}

public sealed class ExecutionReportedDeviceRequest
{
    [Required, StringLength(100, MinimumLength = 1)] public string SourceDeviceKey { get; init; } = "";
    public Guid? DeviceId { get; init; }
    [Required, StringLength(100, MinimumLength = 1)] public string RuntimeTargetCode { get; init; } = "";
    [Required, StringLength(100, MinimumLength = 1)] public string MachineModelCode { get; init; } = "";
}
