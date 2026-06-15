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

            return ApiResult<OrderResult>.Success(OrderResultMapper.ToResult(order), "Order cancelled.");
        }, cancellationToken);

        if (result.Succeeded && result.Data is not null)
        {
            await _publisher.PublishOrderStatusChangedAsync(new OrderStatusChangedEvent
            {
                OrderId = result.Data.Id,
                OrderNumber = result.Data.OrderNumber,
                KioskId = result.Data.KioskId,
                OrganizationId = result.Data.OrganizationId,
                StoreId = result.Data.StoreId,
                OldStatus = fromStatus.ToString(),
                NewStatus = result.Data.Status.ToString(),
                PaymentStatus = result.Data.PaymentStatus.ToString(),
                CustomerStatus = result.Data.CustomerStatus,
                CustomerStatusMessage = result.Data.CustomerStatusMessage,
                CanRetryPayment = result.Data.CanRetryPayment,
                RequiresStaffSupport = result.Data.RequiresStaffSupport,
                UpdatedAt = result.Data.CancelledAt ?? DateTimeOffset.UtcNow,
                Version = 1 // or best effort
            }, cancellationToken);
        }

        return result;
    }

    private static string? NormalizeOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
