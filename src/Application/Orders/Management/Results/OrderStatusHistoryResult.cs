using Domain.Orders.Enums;

namespace Application.Orders.Management.Results;

public sealed class OrderStatusHistoryResult
{
    public Guid Id { get; set; }
    public Guid OrderId { get; set; }
    public Guid? ChangedByAccountId { get; set; }
    public string? ChangedByName { get; set; }
    public string? ChangedByEmail { get; set; }
    public OrderStatus? FromStatus { get; set; }
    public OrderStatus ToStatus { get; set; }
    public string? Reason { get; set; }
    public DateTimeOffset ChangedAt { get; set; }
}
