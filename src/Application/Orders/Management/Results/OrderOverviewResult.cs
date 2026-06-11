namespace Application.Orders.Management.Results;

public class OrderOverviewResult
{
    public int TotalCount { get; set; }
    public List<OrderStatusSummaryDto> ByStatus { get; set; } = new();
    public List<RecentOrderDto> RecentOrders { get; set; } = new();
}

public class OrderStatusSummaryDto
{
    public string Status { get; set; } = null!;
    public int Count { get; set; }
}

public class RecentOrderDto
{
    public Guid OrderId { get; set; }
    public string OrderNumber { get; set; } = null!;
    public Guid KioskId { get; set; }
    public string KioskCode { get; set; } = null!;
    public string Status { get; set; } = null!;
    public string PaymentStatus { get; set; } = null!;
    public decimal TotalAmount { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public string? CustomerStatus { get; set; }
    public string? CustomerStatusMessage { get; set; }
    public bool RequiresStaffSupport { get; set; }
}
