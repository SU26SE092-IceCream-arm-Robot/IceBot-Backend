using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using Application.Devices.Catalog.Commands;
using Application.Devices.ExecutionEndpoints.Commands;
using Application.Devices.Telemetry.Commands;
using Application.Devices.Connectivity.Commands;
using Application.Devices.Credentials.Commands;
using Application.Devices.Catalog.Queries;
using Application.Devices.ExecutionEndpoints.Queries;
using Application.Devices.Telemetry.Queries;
using Application.Devices.Connectivity.Queries;
using Application.Shared.Wrappers;
using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using WebAPI.Configuration.Security;

namespace WebAPI.Controllers.IoT;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/iot/execution-endpoints/{endpointId:guid}/production-sync")]
public sealed class ProductionSyncController : ControllerBase
{
    private readonly IngestProductionEventsBatchCommandHandler _batchHandler;
    private readonly GetProductionEventCheckpointQueryHandler _checkpointHandler;
    private readonly IngestEdgeStateSummariesCommandHandler _summaryHandler;
    private readonly ExecutionEndpointRequestAuthenticator _authenticator;

    public ProductionSyncController(
        IngestProductionEventsBatchCommandHandler batchHandler,
        GetProductionEventCheckpointQueryHandler checkpointHandler,
        IngestEdgeStateSummariesCommandHandler summaryHandler,
        ExecutionEndpointRequestAuthenticator authenticator)
    {
        _batchHandler = batchHandler;
        _checkpointHandler = checkpointHandler;
        _summaryHandler = summaryHandler;
        _authenticator = authenticator;
    }

    [HttpGet("checkpoint")]
    public async Task<IActionResult> GetCheckpoint(
        Guid endpointId,
        [FromQuery] Guid sourceExecutorId,
        CancellationToken cancellationToken)
    {
        var authentication = await _authenticator.AuthenticateAsync(HttpContext, endpointId, cancellationToken);
        if (!authentication.Succeeded)
        {
            return Unauthorized(ApiResult<object>.Fail(authentication.Message, 401));
        }

        var result = await _checkpointHandler.HandleAsync(new GetProductionEventCheckpointQuery
        {
            KioskId = authentication.Endpoint!.KioskId,
            EndpointId = endpointId,
            SourceExecutorId = sourceExecutorId
        }, cancellationToken);
        return StatusCode(result.StatusCode, result);
    }

    [HttpPost("state-summaries")]
    public async Task<IActionResult> IngestStateSummaries(
        Guid endpointId,
        [FromBody] EdgeStateSummarySyncRequest request,
        CancellationToken cancellationToken)
    {
        var authentication = await _authenticator.AuthenticateAsync(HttpContext, endpointId, cancellationToken);
        if (!authentication.Succeeded)
        {
            return Unauthorized(ApiResult<object>.Fail(authentication.Message, 401));
        }

        var result = await _summaryHandler.HandleAsync(new IngestEdgeStateSummariesCommand
        {
            KioskId = authentication.Endpoint!.KioskId,
            EndpointId = endpointId,
            SourceExecutorId = request.SourceExecutorId,
            Summaries = request.Summaries.Select(item => new EdgeStateSummaryItem
            {
                SummaryKind = item.SummaryKind,
                StateRevision = item.StateRevision,
                SummarySchemaVersion = item.SummarySchemaVersion,
                EdgeCreatedAt = item.EdgeCreatedAt,
                PayloadJson = item.Payload?.GetRawText() ?? string.Empty
            }).ToArray()
        }, cancellationToken);
        return StatusCode(result.StatusCode, result);
    }

    [HttpPost("events")]
    public async Task<IActionResult> IngestEvents(
        Guid endpointId,
        [FromBody] ProductionEventBatchRequest request,
        CancellationToken cancellationToken)
    {
        var authentication = await _authenticator.AuthenticateAsync(HttpContext, endpointId, cancellationToken);
        if (!authentication.Succeeded)
            return Unauthorized(ApiResult<object>.Fail(authentication.Message, 401));

        var result = await _batchHandler.HandleAsync(new IngestProductionEventsBatchCommand
        {
            KioskId = authentication.Endpoint!.KioskId,
            EndpointId = endpointId,
            SourceExecutorId = request.SourceExecutorId,
            Events = request.Events.Select(item => new ProductionEventBatchItem
            {
                EventId = item.EventId,
                SequenceNumber = item.SequenceNumber,
                EventType = item.EventType,
                SchemaVersion = item.SchemaVersion,
                EdgeCreatedAt = item.EdgeCreatedAt,
                OrderId = item.OrderId,
                SourceCommandId = item.SourceCommandId,
                ProductionJobId = item.ProductionJobId,
                PayloadJson = item.Payload?.GetRawText()
            }).ToArray()
        }, cancellationToken);
        return StatusCode(result.StatusCode, result);
    }
}

public sealed class ProductionEventBatchRequest
{
    [Required] public Guid SourceExecutorId { get; init; }
    [Required, MinLength(1), MaxLength(100)] public IReadOnlyList<ProductionEventRequestItem> Events { get; init; } = [];
}

public sealed class ProductionEventRequestItem
{
    [Required] public Guid EventId { get; init; }
    [Range(1, long.MaxValue)] public long SequenceNumber { get; init; }
    [Required, StringLength(100)] public string EventType { get; init; } = string.Empty;
    [Range(1, int.MaxValue)] public int SchemaVersion { get; init; } = 1;
    [Required] public DateTimeOffset EdgeCreatedAt { get; init; }
    public Guid? OrderId { get; init; }
    public Guid? SourceCommandId { get; init; }
    public Guid? ProductionJobId { get; init; }
    public JsonElement? Payload { get; init; }
}

public sealed class EdgeStateSummarySyncRequest
{
    [Required] public Guid SourceExecutorId { get; init; }
    [Required, MinLength(1), MaxLength(50)] public IReadOnlyList<EdgeStateSummaryRequestItem> Summaries { get; init; } = [];
}

public sealed class EdgeStateSummaryRequestItem
{
    [Required, StringLength(100)] public string SummaryKind { get; init; } = string.Empty;
    [Range(1, long.MaxValue)] public long StateRevision { get; init; }
    [Range(1, int.MaxValue)] public int SummarySchemaVersion { get; init; } = 1;
    [Required] public DateTimeOffset EdgeCreatedAt { get; init; }
    [Required] public JsonElement? Payload { get; init; }
}
