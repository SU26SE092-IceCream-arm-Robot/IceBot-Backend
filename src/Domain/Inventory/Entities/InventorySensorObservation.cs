using Domain.Common;
using Domain.Inventory.Enums;

namespace Domain.Inventory.Entities;

public sealed class InventorySensorObservation : AppendOnlySyncEntity
{
    public Guid KioskExecutionEndpointId { get; set; }
    public Guid SourceExecutorId { get; set; }
    public Guid SourceEventId { get; set; }
    public Guid IngredientDispenserStateId { get; set; }
    public Guid DeviceId { get; set; }
    public Guid IngredientId { get; set; }
    public long ObservationSequence { get; set; }
    public IngredientLevelStatus ObservedLevelStatus { get; set; }
    public decimal? DerivedEstimatedQuantity { get; set; }
    public DateTimeOffset ObservedAt { get; set; }
    public DateTimeOffset CloudReceivedAt { get; set; }
    public InventorySensorObservationDisposition Disposition { get; set; }
    public string? SensorPayloadJson { get; set; }
}
