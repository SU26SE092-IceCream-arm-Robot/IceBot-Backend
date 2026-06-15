namespace Application.Abstractions.Realtime.Events;

public sealed record PaymentStatusChangedEvent
{
    public string Type => "PaymentStatusChanged";
    public required Guid OrderId { get; init; }
    public required Guid PaymentTransactionId { get; init; }
    public required string OldStatus { get; init; }
    public required string NewStatus { get; init; }
    public required string Provider { get; init; }
    public required DateTimeOffset UpdatedAt { get; init; }
    public required int Version { get; init; }
    public Guid? OrganizationId { get; init; }
    public Guid? StoreId { get; init; }
}
