using Domain.Common;
using Domain.Orders.Enums;

namespace Domain.Orders.Entities;

public sealed class OrderItemStatusHistory : GuidEntity
{
    public Guid OrderItemId { get; init; }
    public Guid? SourceEventId { get; init; }
    public string? SourcePayloadHash { get; init; }
    public OrderItemStatus FromStatus { get; init; }
    public OrderItemStatus ToStatus { get; init; }
    public DateTimeOffset ChangedAt { get; init; }
    public Guid? ChangedByAccountId { get; init; }
    public string? Reason { get; init; }
}
