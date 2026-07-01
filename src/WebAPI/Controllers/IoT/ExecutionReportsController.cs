using Application.EdgeIntegration.Commands;
using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using WebAPI.Configuration.Security;
using Application.Shared.Wrappers;

namespace WebAPI.Controllers.IoT;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/iot/execution-endpoints/{endpointId:guid}/commands/{commandId:guid}/reports")]
public sealed class ExecutionReportsController : ControllerBase
{
    private readonly IngestExecutionReportCommandHandler _ingestExecutionReportHandler;
    private readonly ExecutionEndpointRequestAuthenticator _authenticator;

    public ExecutionReportsController(
        IngestExecutionReportCommandHandler ingestExecutionReportHandler,
        ExecutionEndpointRequestAuthenticator authenticator)
    {
        _ingestExecutionReportHandler = ingestExecutionReportHandler;
        _authenticator = authenticator;
    }

    [HttpPost]
    public async Task<IActionResult> IngestExecutionReport(
        Guid endpointId,
        Guid commandId,
        [FromBody] IngestExecutionReportRequest request,
        CancellationToken cancellationToken)
    {
        var authentication = await _authenticator.AuthenticateAsync(HttpContext, endpointId, cancellationToken);
        if (!authentication.Succeeded) return Unauthorized(ApiResult<object>.Fail(authentication.Message, 401));

        var command = new IngestExecutionReportCommand
        {
            KioskId = authentication.Endpoint!.KioskId,
            EndpointId = endpointId,
            CommandId = commandId,
            SourceEventId = request.SourceEventId,
            SequenceNumber = request.SequenceNumber,
            EdgeCreatedAt = request.EdgeCreatedAt,
            ExecutorReportedAt = request.ExecutorReportedAt,
            ReportType = request.ReportType,
            Status = request.Status,
            DeploymentId = request.DeploymentId,
            SourceProductionJobId = request.SourceProductionJobId,
            WorkcellId = request.WorkcellId,
            ControllerId = request.ControllerId,
            ExecutionPlanChecksum = request.ExecutionPlanChecksum,
            ActiveSetVersion = request.ActiveSetVersion,
            ActiveSetChecksum = request.ActiveSetChecksum,
            SourceConfigurationReleaseId = request.SourceConfigurationReleaseId,
            ReleaseChecksum = request.ReleaseChecksum,
            PhysicalOutputMayHaveOccurred = request.PhysicalOutputMayHaveOccurred,
            ErrorCode = request.ErrorCode,
            ErrorMessage = request.ErrorMessage,
            PayloadJson = request.PayloadJson,
            StockMovements = request.StockMovements.Select(item => new StockMovementEvidenceInput(
                item.SourceEventId,
                item.IngredientDispenserStateId,
                item.QuantityConsumed,
                item.BalanceAfter,
                item.OccurredAt,
                item.IsEstimated)).ToArray()
        };

        var result = await _ingestExecutionReportHandler.HandleAsync(command, cancellationToken);
        return StatusCode(result.StatusCode, result);
    }
}

public sealed class IngestExecutionReportRequest
{
    [Required]
    public Guid SourceEventId { get; init; }

    [Range(1, long.MaxValue)]
    public long SequenceNumber { get; init; }

    [Required]
    public DateTimeOffset EdgeCreatedAt { get; init; }

    public DateTimeOffset? ExecutorReportedAt { get; init; }

    [Required]
    [StringLength(80)]
    public string ReportType { get; init; } = string.Empty;

    [Required]
    [StringLength(80)]
    public string Status { get; init; } = string.Empty;

    public Guid? DeploymentId { get; init; }

    public Guid? SourceProductionJobId { get; init; }

    public Guid? WorkcellId { get; init; }

    public Guid? ControllerId { get; init; }

    [StringLength(500)]
    public string? ExecutionPlanChecksum { get; init; }

    public long? ActiveSetVersion { get; init; }

    [StringLength(500)]
    public string? ActiveSetChecksum { get; init; }

    public Guid? SourceConfigurationReleaseId { get; init; }

    [StringLength(500)]
    public string? ReleaseChecksum { get; init; }

    public bool? PhysicalOutputMayHaveOccurred { get; init; }

    [StringLength(100)]
    public string? ErrorCode { get; init; }

    [StringLength(1000)]
    public string? ErrorMessage { get; init; }

    public string? PayloadJson { get; init; }

    public IReadOnlyCollection<StockMovementEvidenceRequest> StockMovements { get; init; } = [];
}

public sealed class StockMovementEvidenceRequest
{
    [Required]
    public Guid SourceEventId { get; init; }

    [Required]
    public Guid IngredientDispenserStateId { get; init; }

    [Range(typeof(decimal), "0.00000001", "79228162514264337593543950335")]
    public decimal QuantityConsumed { get; init; }

    [Range(typeof(decimal), "0", "79228162514264337593543950335")]
    public decimal? BalanceAfter { get; init; }

    public DateTimeOffset? OccurredAt { get; init; }

    public bool IsEstimated { get; init; }
}
