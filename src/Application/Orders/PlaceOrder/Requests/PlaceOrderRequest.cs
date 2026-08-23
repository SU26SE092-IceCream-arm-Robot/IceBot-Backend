namespace Application.Orders.PlaceOrder.Requests;

public sealed class PlaceOrderRequest
{
    public string? ClientOrderId { get; set; }

    public decimal? ClientTotalAmount { get; set; }

    public string? CustomerName { get; set; }

    public string? CustomerPhoneNumber { get; set; }

    public string? Notes { get; set; }

    public List<PlaceOrderItemRequest> Items { get; set; } = new();
}
