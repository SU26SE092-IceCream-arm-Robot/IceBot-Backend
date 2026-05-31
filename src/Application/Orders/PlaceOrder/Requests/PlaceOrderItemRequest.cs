namespace Application.Orders.PlaceOrder.Requests;

public sealed class PlaceOrderItemRequest
{
    public Guid MenuItemId { get; set; }

    public string? ClientLineId { get; set; }

    public int Quantity { get; set; }

    public string? OptionsJson { get; set; }
}
