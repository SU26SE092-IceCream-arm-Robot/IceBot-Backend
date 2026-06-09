using Application.Orders.PlaceOrder.Requests;
using System;

namespace Application.Orders.PlaceOrder.Commands;

public sealed class CancelPendingOrderCommand
{
    public Guid OrderId { get; init; }
    public CancelPendingOrderRequest Request { get; init; } = null!;
}
