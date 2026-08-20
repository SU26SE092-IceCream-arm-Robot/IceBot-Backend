using Application.Orders.IncidentResolution;
using Asp.Versioning;
using Domain.Orders.Incidents;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using WebAPI.Authorization;

namespace WebAPI.Controllers.Orders;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/management")]
[Authorize(Policy = "orders.intervention.manage")]
public sealed class ManagementProductionIncidentsController(
    ListProductionIncidentsQueryHandler listHandler,
    GetProductionIncidentQueryHandler getHandler,
    OpenProductionIncidentCommandHandler openHandler,
    RecordProductionInspectionCommandHandler inspectionHandler,
    SelectProductionIncidentResolutionCommandHandler resolutionHandler,
    CompleteProductionIncidentCommandHandler completeHandler) : ControllerBase
{
    [HttpGet("production-incidents")]
    public async Task<IActionResult> List(
        [FromQuery] ProductionIncidentStatus? status,
        [FromQuery] Guid? organizationId,
        [FromQuery] Guid? storeId,
        [FromQuery] Guid? kioskId,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var result = await listHandler.HandleAsync(new ListProductionIncidentsQuery(
            User.GetUserContext(), status, organizationId, storeId, kioskId, pageNumber, pageSize), ct);
        return StatusCode(result.StatusCode, result);
    }

    [HttpGet("orders/{orderId:guid}/production-incidents/{incidentId:guid}")]
    public async Task<IActionResult> Get(Guid orderId, Guid incidentId, CancellationToken ct)
    {
        var result = await getHandler.HandleAsync(new GetProductionIncidentQuery(
            User.GetUserContext(), orderId, incidentId), ct);
        return StatusCode(result.StatusCode, result);
    }

    [HttpPost("orders/{orderId:guid}/items/{orderItemId:guid}/production-incidents")]
    public async Task<IActionResult> Open(Guid orderId, Guid orderItemId,
        [FromBody] OpenProductionIncidentRequest request, CancellationToken ct)
    {
        var result = await openHandler.HandleAsync(new OpenProductionIncidentCommand(
            User.GetUserContext(), orderId, orderItemId, request.SourceCommandId,
            request.SourceProductionJobId, request.ProductionUnitNo,
            request.ProductionUnitQuantity, request.Reason), ct);
        return StatusCode(result.StatusCode, result);
    }

    [HttpPatch("orders/{orderId:guid}/production-incidents/{incidentId:guid}/inspection")]
    public async Task<IActionResult> Inspect(Guid orderId, Guid incidentId,
        [FromBody] RecordProductionInspectionRequest request, CancellationToken ct)
    {
        var result = await inspectionHandler.HandleAsync(new RecordProductionInspectionCommand(
            User.GetUserContext(), orderId, incidentId, request.Outcome, request.Reason), ct);
        return StatusCode(result.StatusCode, result);
    }

    [HttpPost("orders/{orderId:guid}/production-incidents/{incidentId:guid}/resolution")]
    public async Task<IActionResult> SelectResolution(Guid orderId, Guid incidentId,
        [FromBody] SelectProductionIncidentResolutionRequest request, CancellationToken ct)
    {
        var result = await resolutionHandler.HandleAsync(new SelectProductionIncidentResolutionCommand(
            User.GetUserContext(), orderId, incidentId, request.ResolutionRequestId,
            request.Resolution, request.Reason, request.PaymentTransactionId,
            request.VoucherCode, request.VoucherValue, request.AcknowledgeFullOrderCompensation), ct);
        return StatusCode(result.StatusCode, result);
    }

    [HttpPatch("orders/{orderId:guid}/production-incidents/{incidentId:guid}/complete")]
    public async Task<IActionResult> Complete(Guid orderId, Guid incidentId,
        [FromBody] CompleteProductionIncidentRequest request, CancellationToken ct)
    {
        var result = await completeHandler.HandleAsync(new CompleteProductionIncidentCommand(
            User.GetUserContext(), orderId, incidentId, request.Notes), ct);
        return StatusCode(result.StatusCode, result);
    }
}

public sealed class OpenProductionIncidentRequest
{
    public Guid SourceCommandId { get; init; }
    public Guid SourceProductionJobId { get; init; }
    [Range(1, int.MaxValue)] public int ProductionUnitNo { get; init; }
    [Range(1, int.MaxValue)] public int ProductionUnitQuantity { get; init; }
    [Required, StringLength(1000)] public string Reason { get; init; } = null!;
}

public sealed class RecordProductionInspectionRequest
{
    public ProductionInspectionOutcome Outcome { get; init; }
    [Required, StringLength(1000)] public string Reason { get; init; } = null!;
}

public sealed class SelectProductionIncidentResolutionRequest
{
    public Guid ResolutionRequestId { get; init; }
    public ProductionIncidentResolution Resolution { get; init; }
    [Required, StringLength(1000)] public string Reason { get; init; } = null!;
    public Guid? PaymentTransactionId { get; init; }
    [StringLength(100)] public string? VoucherCode { get; init; }
    [Range(typeof(decimal), "0.01", "79228162514264337593543950335")] public decimal? VoucherValue { get; init; }
    public bool AcknowledgeFullOrderCompensation { get; init; }
}

public sealed class CompleteProductionIncidentRequest
{
    [Required, StringLength(1000)] public string Notes { get; init; } = null!;
}
