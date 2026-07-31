namespace Application.Inventory.Observations;

public sealed class InventorySensorObservationIngestResult
{
    public int AppliedCount { get; init; }
    public int DuplicateCount { get; init; }
    public int OutOfOrderCount { get; init; }
}
