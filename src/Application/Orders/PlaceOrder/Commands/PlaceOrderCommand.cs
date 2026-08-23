using Application.Orders.PlaceOrder.Requests;

namespace Application.Orders.PlaceOrder.Commands;

public sealed class PlaceOrderCommand
{
    public Guid KioskId { get; init; }
    public Guid SourceClientDeviceId { get; init; }
    public PlaceOrderRequest Request { get; init; } = null!;
    public string? IdempotencyKey { get; init; }
}
