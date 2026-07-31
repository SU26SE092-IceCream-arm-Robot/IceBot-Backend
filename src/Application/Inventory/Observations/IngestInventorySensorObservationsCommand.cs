using System.Text.Json;
using Domain.Inventory.Enums;

namespace Application.Inventory.Observations;

public sealed class IngestInventorySensorObservationsCommand
{
    public Guid KioskId { get; init; }
    public Guid EndpointId { get; init; }
    public Guid SourceExecutorId { get; init; }
    public IReadOnlyList<InventorySensorObservationInput> Observations { get; init; } = [];
}

public sealed class InventorySensorObservationInput
{
    public Guid SourceEventId { get; init; }
    public Guid IngredientDispenserStateId { get; init; }
    public Guid DeviceId { get; init; }
    public long ObservationSequence { get; init; }
    public IngredientLevelStatus ObservedLevelStatus { get; init; }
    public DateTimeOffset ObservedAt { get; init; }
    public JsonElement? SensorPayload { get; init; }
}
