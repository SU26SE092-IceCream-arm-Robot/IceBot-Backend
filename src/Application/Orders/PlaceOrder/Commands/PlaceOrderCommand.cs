using Application.Orders.PlaceOrder.Requests;

namespace Application.Orders.PlaceOrder.Commands;

public sealed class PlaceOrderCommand
{
    public PlaceOrderRequest Request { get; init; } = null!;
}
