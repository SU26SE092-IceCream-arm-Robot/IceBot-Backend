namespace Application.Abstractions.Realtime.Events;

public sealed record OrderItemFulfillmentChangedEvent
{
    public string Type => "OrderItemFulfillmentChanged";
    public required Guid OrderId { get; init; }
    public required Guid OrderItemId { get; init; }
    public required string OrderNumber { get; init; }
    public required Guid KioskId { get; init; }
    public Guid? OrganizationId { get; init; }
    public Guid? StoreId { get; init; }
    public required string FulfillmentType { get; init; }
    public required string OldStatus { get; init; }
    public required string NewStatus { get; init; }
    public required int Quantity { get; init; }
    public required DateTimeOffset UpdatedAt { get; init; }
    public required int Version { get; init; }
}
