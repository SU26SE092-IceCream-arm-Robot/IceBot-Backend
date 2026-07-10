using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using Application.Devices.Catalog.Commands;
using Application.Devices.ExecutionEndpoints.Commands;
using Application.Devices.Telemetry.Commands;
using Application.Devices.Connectivity.Commands;
using Application.Devices.Credentials.Commands;
using Application.Shared.Wrappers;
using Asp.Versioning;
using Domain.Common.Enums;
using Microsoft.AspNetCore.Mvc;
using WebAPI.Configuration.Security;

namespace WebAPI.Controllers.IoT;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/iot/execution-endpoints/{endpointId:guid}/device-events")]
public sealed class DeviceEventsController : ControllerBase
{
    private readonly IngestDeviceEventCommandHandler _handler;
    private readonly ExecutionEndpointRequestAuthenticator _authenticator;

    public DeviceEventsController(
        IngestDeviceEventCommandHandler handler,
        ExecutionEndpointRequestAuthenticator authenticator)
    {
        _handler = handler;
        _authenticator = authenticator;
    }

    [HttpPost]
    public async Task<IActionResult> IngestDeviceEvent(
        Guid endpointId,
        [FromBody] IngestDeviceEventRequest request,
        CancellationToken cancellationToken)
    {
        var authentication = await _authenticator.AuthenticateAsync(HttpContext, endpointId, cancellationToken);
        if (!authentication.Succeeded)
        {
            return Unauthorized(ApiResult<object>.Fail(authentication.Message, 401));
        }

        var result = await _handler.HandleAsync(new IngestDeviceEventCommand
        {
            KioskId = authentication.Endpoint!.KioskId,
            EndpointId = endpointId,
            OriginNodeId = request.OriginNodeId,
            DeviceId = request.DeviceId,
            EventId = request.EventId,
            CorrelationId = request.CorrelationId,
            CausationId = request.CausationId,
            EventType = request.EventType,
            Severity = request.Severity,
            Message = request.Message,
            OccurredAt = request.OccurredAt,
            PayloadJson = request.Payload?.GetRawText()
        }, cancellationToken);
        return StatusCode(result.StatusCode, result);
    }
}

public sealed class IngestDeviceEventRequest
{
    [Required]
    public Guid OriginNodeId { get; init; }

    [Required]
    public Guid DeviceId { get; init; }

    [Required]
    public Guid EventId { get; init; }

    public Guid? CorrelationId { get; init; }

    public Guid? CausationId { get; init; }

    [Required]
    [StringLength(100)]
    public string EventType { get; init; } = string.Empty;

    public SeverityLevel Severity { get; init; }

    [Required]
    [StringLength(1000)]
    public string Message { get; init; } = string.Empty;

    [Required]
    public DateTimeOffset OccurredAt { get; init; }

    public JsonElement? Payload { get; init; }
}
