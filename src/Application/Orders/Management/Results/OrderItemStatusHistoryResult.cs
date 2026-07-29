using Domain.Orders.Enums;

namespace Application.Orders.Management.Results;

public sealed class OrderItemStatusHistoryResult
{
    public Guid Id { get; init; }
    public Guid OrderItemId { get; init; }
    public Guid? ChangedByAccountId { get; init; }
    public string? ChangedByName { get; init; }
    public string? ChangedByEmail { get; init; }
    public OrderItemStatus FromStatus { get; init; }
    public OrderItemStatus ToStatus { get; init; }
    public string? Reason { get; init; }
    public DateTimeOffset ChangedAt { get; init; }
}
