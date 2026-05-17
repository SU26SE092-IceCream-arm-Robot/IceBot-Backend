using Domain.Common;
using Domain.Identity.Entities;
using Domain.Orders.Enums;

namespace Domain.Orders.Entities;

public partial class OrderStatusHistory : BusinessEntity
{
    public Guid OrderId { get; set; }

    public Guid? ChangedByAccountId { get; set; }

    public OrderStatus? FromStatus { get; set; }

    public OrderStatus ToStatus { get; set; }

    public string? Reason { get; set; }

    public DateTimeOffset ChangedAt { get; set; }

    public virtual Account? ChangedByAccount { get; set; }

    public virtual Order Order { get; set; } = null!;
}
