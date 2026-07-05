using Application.Abstractions.Realtime;
using Application.Abstractions.Realtime.Events;
using Application.Orders.Abstractions;
using Application.Orders.PlaceOrder.Mapping;
using Application.Orders.PlaceOrder.Results;
using Application.Shared.Wrappers;
using Application.Tenants;
using Domain.Orders.Entities;
using Domain.Orders.Enums;

namespace Application.Orders.Management.Commands;

public sealed class CancelManagementOrderCommandHandler
{
    private readonly IOrderStore _orderStore;
    private readonly IRealtimeNotificationPublisher _publisher;

    public CancelManagementOrderCommandHandler(IOrderStore orderStore, IRealtimeNotificationPublisher publisher)
    {
        _orderStore = orderStore;
        _publisher = publisher;
    }

    public async Task<ApiResult<OrderResult>> HandleAsync(
        CancelManagementOrderCommand command,
        CancellationToken cancellationToken = default)
    {
        OrderStatus fromStatus = OrderStatus.Draft;
        OrderStatusChangedEvent? statusChangedEvent = null;

        var result = await _orderStore.ExecuteInTransactionAsync(async ct =>
        {
            var order = await _orderStore.GetOrderByIdAsync(command.OrderId, ct);
            if (order is null)
            {
                return ApiResult<OrderResult>.Fail("Order not found.", 404);
            }

            fromStatus = order.Status;

            if (!ScopeAccessRules.CanAccessScopedRow(ScopeRoleSets.OrdersManage,
                command.UserContext,
                order.OrganizationId,
                order.StoreId,
                order.KioskId))
            {
                return ApiResult<OrderResult>.Fail("Access denied.", 403);
            }

            if (order.PaymentStatus == PaymentStatus.Paid)
            {
                return ApiResult<OrderResult>.Fail(
                    "Paid orders cannot be cancelled directly. Please flag them as RefundRequired instead.",
                    409);
            }

            if (order.Status == OrderStatus.Completed || order.Status == OrderStatus.Cancelled)
            {
                return ApiResult<OrderResult>.Fail(
                    $"Cannot cancel an order in state '{order.Status}'.",
                    409);
            }

            var now = DateTimeOffset.UtcNow;
            var reason = string.IsNullOrWhiteSpace(command.Reason) ? null : command.Reason.Trim();

            order.Cancel(now, reason);
            order.PaymentStatus = PaymentStatus.Cancelled;
            order.UpdatedAt = now;

            var history = new OrderStatusHistory
            {
                Id = Guid.NewGuid(),
                OrderId = order.Id,
                FromStatus = fromStatus,
                ToStatus = OrderStatus.Cancelled,
                ChangedByAccountId = command.UserContext.AccountId,
                Reason = reason ?? "Cancelled by back-office management.",
                ChangedAt = now
            };
            await _orderStore.AddOrderStatusHistoryAsync(history, ct);

            await _orderStore.SaveChangesAsync(ct);

            var orderResult = OrderResultMapper.ToResult(order);
            statusChangedEvent = CreateStatusChangedEvent(order, orderResult, fromStatus);
            return ApiResult<OrderResult>.Success(
                orderResult,
                "Order cancelled successfully.");
        }, cancellationToken);

        if (result.Succeeded && statusChangedEvent is not null)
        {
            await _publisher.PublishOrderStatusChangedAsync(statusChangedEvent, cancellationToken);
        }

        return result;
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
