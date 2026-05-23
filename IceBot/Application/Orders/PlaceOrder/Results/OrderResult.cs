using Domain.Orders.Enums;

namespace Application.Orders.PlaceOrder.Results;

public sealed class OrderResult
{
    public Guid Id { get; set; }

    public Guid KioskId { get; set; }

    public Guid? StoreId { get; set; }

    public Guid? OrganizationId { get; set; }

    public string OrderNumber { get; set; } = null!;

    public string? ClientOrderId { get; set; }

    public OrderChannel Channel { get; set; }

    public OrderStatus Status { get; set; }

    public PaymentStatus PaymentStatus { get; set; }

    public string Currency { get; set; } = "VND";

    public decimal SubtotalAmount { get; set; }

    public decimal DiscountAmount { get; set; }

    public decimal TaxAmount { get; set; }

    public decimal TotalAmount { get; set; }

    public decimal PaidAmount { get; set; }

    public DateTimeOffset PlacedAt { get; set; }

    public DateTimeOffset? PaidAt { get; set; }

    public DateTimeOffset? CompletedAt { get; set; }

    public DateTimeOffset? CancelledAt { get; set; }

    public List<OrderItemResult> Items { get; set; } = new();
}
