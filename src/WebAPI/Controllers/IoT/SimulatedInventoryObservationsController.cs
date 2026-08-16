using Application.Inventory.Observations;
using Application.Shared.Wrappers;
using Asp.Versioning;
using Domain.Inventory.Enums;
using Microsoft.AspNetCore.Mvc;
using WebAPI.Configuration.Security;

namespace WebAPI.Controllers.IoT;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/iot/execution-endpoints/{endpointId:guid}/simulated-inventory-observations")]
public sealed class SimulatedInventoryObservationsController(
    IHostEnvironment environment,
    IngestInventorySensorObservationsCommandHandler handler,
    ExecutionEndpointRequestAuthenticator authenticator) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Ingest(
        Guid endpointId,
        [FromBody] SimulatedInventoryObservationsRequest request,
        CancellationToken cancellationToken)
    {
        if (!environment.IsDevelopment())
            return NotFound();

        var authentication = await authenticator.AuthenticateAsync(HttpContext, endpointId, cancellationToken);
        if (!authentication.Succeeded)
            return Unauthorized(ApiResult<object>.Fail(authentication.Message, 401));

        var result = await handler.HandleAsync(new IngestInventorySensorObservationsCommand
        {
            KioskId = authentication.Endpoint!.KioskId,
            EndpointId = endpointId,
            SourceExecutorId = request.SourceExecutorId,
            Observations = request.Observations.Select(item => new InventorySensorObservationInput
            {
                SourceEventId = item.SourceEventId,
                IngredientDispenserStateId = item.IngredientDispenserStateId,
                DeviceId = item.DeviceId,
                ObservationSequence = item.ObservationSequence,
                ObservedLevelStatus = item.ObservedLevelStatus,
                ObservedAt = item.ObservedAt
            }).ToArray()
        }, cancellationToken);
        return StatusCode(result.StatusCode, result);
    }
}

public sealed class SimulatedInventoryObservationsRequest
{
    public Guid SourceExecutorId { get; init; }
    public IReadOnlyList<SimulatedInventoryObservationRequest> Observations { get; init; } = [];
}

public sealed class SimulatedInventoryObservationRequest
{
    public Guid SourceEventId { get; init; }
    public Guid IngredientDispenserStateId { get; init; }
    public Guid DeviceId { get; init; }
    public long ObservationSequence { get; init; }
    public IngredientLevelStatus ObservedLevelStatus { get; init; }
    public DateTimeOffset ObservedAt { get; init; }
}
