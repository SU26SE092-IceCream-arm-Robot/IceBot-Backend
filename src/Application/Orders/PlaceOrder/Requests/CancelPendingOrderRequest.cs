namespace Application.Orders.PlaceOrder.Requests;

public sealed class CancelPendingOrderRequest
{
    public string? Reason { get; set; }
}
