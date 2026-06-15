namespace Application.Abstractions.Realtime.Events;

public sealed record OrderStatusChangedEvent
{
    public string Type => "OrderStatusChanged";
    public required Guid OrderId { get; init; }
    public required string OrderNumber { get; init; }
    public required Guid KioskId { get; init; }
    public Guid? OrganizationId { get; init; }
    public Guid? StoreId { get; init; }
    public required string OldStatus { get; init; }
    public required string NewStatus { get; init; }
    public required string PaymentStatus { get; init; }
    public required string CustomerStatus { get; init; }
    public string? CustomerStatusMessage { get; init; }
    public required bool CanRetryPayment { get; init; }
    public required bool RequiresStaffSupport { get; init; }
    public required DateTimeOffset UpdatedAt { get; init; }
    public required int Version { get; init; }
}
