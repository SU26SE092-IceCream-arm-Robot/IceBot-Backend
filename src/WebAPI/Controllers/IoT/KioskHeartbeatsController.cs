using System.ComponentModel.DataAnnotations;
using Application.Devices.Commands;
using Application.Shared.Wrappers;
using Asp.Versioning;
using Domain.Devices.Enums;
using Microsoft.AspNetCore.Mvc;
using WebAPI.Configuration.Security;

namespace WebAPI.Controllers.IoT;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/iot/execution-endpoints/{endpointId:guid}/heartbeat")]
public sealed class KioskHeartbeatsController : ControllerBase
{
    private readonly IngestKioskHeartbeatCommandHandler _handler;
    private readonly ExecutionEndpointRequestAuthenticator _authenticator;

    public KioskHeartbeatsController(
        IngestKioskHeartbeatCommandHandler handler,
        ExecutionEndpointRequestAuthenticator authenticator)
    {
        _handler = handler;
        _authenticator = authenticator;
    }

    [HttpPost]
    public async Task<IActionResult> IngestHeartbeat(
        Guid endpointId,
        [FromBody] IngestKioskHeartbeatRequest request,
        CancellationToken cancellationToken)
    {
        var authentication = await _authenticator.AuthenticateAsync(
            HttpContext,
            endpointId,
            cancellationToken);
        if (!authentication.Succeeded)
        {
            return Unauthorized(ApiResult<object>.Fail(authentication.Message, 401));
        }

        var result = await _handler.HandleAsync(new IngestKioskHeartbeatCommand
        {
            KioskId = authentication.Endpoint!.KioskId,
            EndpointId = endpointId,
            OriginNodeId = request.OriginNodeId,
            HeartbeatSequence = request.HeartbeatSequence,
            ReportedAt = request.ReportedAt,
            Status = request.Status,
            RobotStatus = request.RobotStatus,
            NetworkStatus = request.NetworkStatus,
            AppVersion = request.AppVersion,
            FirmwareVersion = request.FirmwareVersion,
            CpuUsagePercent = request.CpuUsagePercent,
            MemoryUsagePercent = request.MemoryUsagePercent,
            DiskUsagePercent = request.DiskUsagePercent,
            PendingSyncEventCount = request.PendingSyncEventCount
        }, cancellationToken);
        return StatusCode(result.StatusCode, result);
    }
}

public sealed class IngestKioskHeartbeatRequest
{
    [Required]
    public Guid OriginNodeId { get; init; }

    [Range(1, long.MaxValue)]
    public long HeartbeatSequence { get; init; }

    [Required]
    public DateTimeOffset ReportedAt { get; init; }

    public KioskHeartbeatStatus Status { get; init; } = KioskHeartbeatStatus.Online;

    [StringLength(100)]
    public string? RobotStatus { get; init; }

    [StringLength(100)]
    public string? NetworkStatus { get; init; }

    [StringLength(100)]
    public string? AppVersion { get; init; }

    [StringLength(100)]
    public string? FirmwareVersion { get; init; }

    [Range(typeof(decimal), "0", "100")]
    public decimal? CpuUsagePercent { get; init; }

    [Range(typeof(decimal), "0", "100")]
    public decimal? MemoryUsagePercent { get; init; }

    [Range(typeof(decimal), "0", "100")]
    public decimal? DiskUsagePercent { get; init; }

    [Range(0, int.MaxValue)]
    public int PendingSyncEventCount { get; init; }
}
