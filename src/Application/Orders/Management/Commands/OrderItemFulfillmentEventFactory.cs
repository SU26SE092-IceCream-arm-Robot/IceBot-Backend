using Application.Abstractions.Realtime.Events;
using Domain.Orders.Entities;
using Domain.Orders.Enums;

namespace Application.Orders.Management.Commands;

internal static class OrderItemFulfillmentEventFactory
{
    public static OrderItemFulfillmentChangedEvent Create(
        Order order,
        OrderItem item,
        OrderItemStatus oldStatus,
        DateTimeOffset changedAt) => new()
        {
            OrderId = order.Id,
            OrderItemId = item.Id,
            OrderNumber = order.OrderNumber,
            KioskId = order.KioskId,
            OrganizationId = order.OrganizationId,
            StoreId = order.StoreId,
            FulfillmentType = item.FulfillmentType.ToString(),
            OldStatus = oldStatus.ToString(),
            NewStatus = item.Status.ToString(),
            Quantity = item.Quantity,
            UpdatedAt = changedAt,
            Version = 1
        };
}
