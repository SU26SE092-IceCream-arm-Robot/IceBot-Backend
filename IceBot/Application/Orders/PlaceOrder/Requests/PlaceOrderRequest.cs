using Domain.Orders.Enums;

namespace Application.Orders.PlaceOrder.Requests;

public sealed class PlaceOrderRequest
{
    public Guid KioskId { get; set; }

    public string? IdempotencyKey { get; set; }

    public string? ClientOrderId { get; set; }

    public Guid? RuntimeSnapshotId { get; set; }

    public DateTimeOffset? RuntimeSnapshotGeneratedAt { get; set; }

    public decimal? ClientTotalAmount { get; set; }

    public OrderChannel Channel { get; set; } = OrderChannel.Tablet;

    public string? CustomerName { get; set; }

    public string? CustomerPhoneNumber { get; set; }

    public string? Notes { get; set; }

    public List<PlaceOrderItemRequest> Items { get; set; } = new();
}
