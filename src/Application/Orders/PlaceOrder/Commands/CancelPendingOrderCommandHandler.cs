using Application.Abstractions.Realtime;
using Application.Abstractions.Realtime.Events;
using Application.Orders.Abstractions;
using Application.Orders.PlaceOrder.Mapping;
using Application.Orders.PlaceOrder.Results;
using Application.Shared.Wrappers;
using Domain.Orders.Entities;
using Domain.Orders.Enums;

namespace Application.Orders.PlaceOrder.Commands;

public sealed class CancelPendingOrderCommandHandler
{
    private readonly IOrderStore _orderStore;
    private readonly IRealtimeNotificationPublisher _publisher;

    public CancelPendingOrderCommandHandler(IOrderStore orderStore, IRealtimeNotificationPublisher publisher)
    {
        _orderStore = orderStore;
        _publisher = publisher;
    }

    public async Task<ApiResult<OrderResult>> HandleAsync(
        CancelPendingOrderCommand command,
        CancellationToken cancellationToken = default)
    {
        var orderId = command.OrderId;
        var request = command.Request;
        OrderStatus fromStatus = OrderStatus.Draft;
        OrderStatusChangedEvent? statusChangedEvent = null;

        var result = await _orderStore.ExecuteInTransactionAsync(async ct =>
        {
            var order = await _orderStore.GetOrderByIdAsync(orderId, ct);
            if (order is null)
            {
                return ApiResult<OrderResult>.Fail("Order not found.", 404);
            }

            fromStatus = order.Status;

            if (order.PaymentStatus == PaymentStatus.Paid)
            {
                return ApiResult<OrderResult>.Fail("Paid orders cannot be cancelled through this endpoint.", 409);
            }

            if (order.Status is not (OrderStatus.Draft or OrderStatus.PendingPayment))
            {
                return ApiResult<OrderResult>.Fail("Only draft or pending-payment orders can be cancelled.", 409);
            }
            var now = DateTimeOffset.UtcNow;
            var reason = NormalizeOptional(request.Reason);

            order.Cancel(now, reason);
            order.PaymentStatus = PaymentStatus.Cancelled;
            order.UpdatedAt = now;

            var history = new OrderStatusHistory
            {
                Id = Guid.NewGuid(),
                OrderId = order.Id,
                FromStatus = fromStatus,
                ToStatus = Domain.Orders.Enums.OrderStatus.Cancelled,
                ChangedAt = now,
                Reason = reason ?? "Order cancelled by customer."
            };
            await _orderStore.AddOrderStatusHistoryAsync(history, ct);

            await _orderStore.SaveChangesAsync(ct);
            var orderResult = OrderResultMapper.ToResult(order);
            statusChangedEvent = CreateStatusChangedEvent(order, orderResult, fromStatus);
            return ApiResult<OrderResult>.Success(orderResult, "Order cancelled.");
        }, cancellationToken);

        if (result.Succeeded && statusChangedEvent is not null)
        {
            await _publisher.PublishOrderStatusChangedAsync(statusChangedEvent, cancellationToken);
        }

        return result;
    }

    private static string? NormalizeOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private static OrderStatusChangedEvent CreateStatusChangedEvent(
        Order order,
        OrderResult result,
        OrderStatus fromStatus) => new()
    {
        OrderId = order.Id,
        OrderNumber = order.OrderNumber,
        KioskId = order.KioskId,
        OrganizationId = order.OrganizationId,
        StoreId = order.StoreId,
        OldStatus = fromStatus.ToString(),
        NewStatus = result.Status.ToString(),
        PaymentStatus = result.PaymentStatus.ToString(),
        CustomerStatus = result.CustomerStatus,
        CustomerStatusMessage = result.CustomerStatusMessage,
        CanRetryPayment = result.CanRetryPayment,
        RequiresStaffSupport = result.RequiresStaffSupport,
        UpdatedAt = result.CancelledAt ?? DateTimeOffset.UtcNow,
        Version = 1
    };
}
