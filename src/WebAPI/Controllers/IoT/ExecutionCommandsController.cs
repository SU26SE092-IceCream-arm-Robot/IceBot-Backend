using Application.EdgeIntegration.Commands;
using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using WebAPI.Configuration.Security;
using Application.Shared.Wrappers;

namespace WebAPI.Controllers.IoT;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/iot/execution-endpoints/{endpointId:guid}/commands")]
public sealed class ExecutionCommandsController : ControllerBase
{
    private readonly PullEdgeCommandsCommandHandler _pullCommandsHandler;
    private readonly AcknowledgeEdgeCommandCommandHandler _acknowledgeCommandHandler;
    private readonly ExecutionEndpointRequestAuthenticator _authenticator;

    public ExecutionCommandsController(
        PullEdgeCommandsCommandHandler pullCommandsHandler,
        AcknowledgeEdgeCommandCommandHandler acknowledgeCommandHandler,
        ExecutionEndpointRequestAuthenticator authenticator)
    {
        _pullCommandsHandler = pullCommandsHandler;
        _acknowledgeCommandHandler = acknowledgeCommandHandler;
        _authenticator = authenticator;
    }

    [HttpPost("pull")]
    public async Task<IActionResult> PullCommands(
        Guid endpointId,
        [FromBody] PullEdgeCommandsRequest request,
        CancellationToken cancellationToken)
    {
        var authentication = await _authenticator.AuthenticateAsync(HttpContext, endpointId, cancellationToken);
        if (!authentication.Succeeded) return Unauthorized(ApiResult<object>.Fail(authentication.Message, 401));

        var command = new PullEdgeCommandsCommand
        {
            KioskId = authentication.Endpoint!.KioskId,
            EndpointId = endpointId,
            MaxCommands = request.MaxCommands,
            EdgeTime = request.EdgeTime
        };

        var result = await _pullCommandsHandler.HandleAsync(command, cancellationToken);
        return StatusCode(result.StatusCode, result);
    }

    [HttpPost("{commandId:guid}/ack")]
    public async Task<IActionResult> AcknowledgeCommand(
        Guid endpointId,
        Guid commandId,
        [FromBody] AcknowledgeEdgeCommandRequest request,
        CancellationToken cancellationToken)
    {
        var authentication = await _authenticator.AuthenticateAsync(HttpContext, endpointId, cancellationToken);
        if (!authentication.Succeeded) return Unauthorized(ApiResult<object>.Fail(authentication.Message, 401));

        var command = new AcknowledgeEdgeCommandCommand
        {
            KioskId = authentication.Endpoint!.KioskId,
            EndpointId = endpointId,
            CommandId = commandId,
            AckStatus = request.AckStatus,
            AcknowledgedAt = request.AcknowledgedAt,
            RejectionCode = request.RejectionCode,
            RejectionMessage = request.RejectionMessage,
            PhysicalOutputMayHaveOccurred = request.PhysicalOutputMayHaveOccurred
        };

        var result = await _acknowledgeCommandHandler.HandleAsync(command, cancellationToken);
        return StatusCode(result.StatusCode, result);
    }
}

public sealed class PullEdgeCommandsRequest
{
    [Range(1, 20)]
    public int MaxCommands { get; init; } = 10;

    public DateTimeOffset? EdgeTime { get; init; }
}

public sealed class AcknowledgeEdgeCommandRequest
{
    [Required]
    public string AckStatus { get; init; } = string.Empty;

    public DateTimeOffset? AcknowledgedAt { get; init; }

    [StringLength(100)]
    public string? RejectionCode { get; init; }

    [StringLength(500)]
    public string? RejectionMessage { get; init; }

    public bool? PhysicalOutputMayHaveOccurred { get; init; }
}
