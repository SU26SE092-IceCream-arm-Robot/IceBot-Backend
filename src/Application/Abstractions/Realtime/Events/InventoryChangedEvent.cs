namespace Application.Abstractions.Realtime.Events;

public sealed record InventoryChangedEvent
{
    public string Type => "InventoryChanged";
    public required Guid DispenserStateId { get; init; }
    public required Guid KioskId { get; init; }
    public Guid? OrganizationId { get; init; }
    public Guid? StoreId { get; init; }
    public required string IngredientName { get; init; }
    public required decimal? EstimatedQuantity { get; init; }
    public required string Unit { get; init; }
    public required string Status { get; init; }
    public required DateTimeOffset UpdatedAt { get; init; }
    public required int Version { get; init; }
}
