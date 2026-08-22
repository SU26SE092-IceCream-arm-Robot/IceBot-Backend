using Application.Abstractions.Realtime;
using Application.Abstractions.Realtime.Events;
using Application.Orders.Abstractions;
using Application.Orders.Management.Mapping;
using Application.Orders.Management.Results;
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

    public async Task<ApiResult<ManagementOrderDetailResult>> HandleAsync(
        CancelManagementOrderCommand command,
        CancellationToken cancellationToken = default)
    {
        OrderStatus fromStatus = OrderStatus.Draft;
        OrderStatusChangedEvent? statusChangedEvent = null;
        var scope = ScopeAccessRules.GetEffectiveScope(ScopeRoleSets.OrdersInterventionManage, command.UserContext);

        var result = await _orderStore.ExecuteInTransactionAsync(async ct =>
        {
            await _orderStore.AcquireOrderWorkflowLockAsync(command.OrderId, ct);
            var order = await _orderStore.GetManagementOrderByIdAsync(
                command.OrderId, command.UserContext.IsSystemAdmin,
                scope.OrganizationIds, scope.StoreIds, scope.KioskIds, ct);
            if (order is null)
            {
                return ApiResult<ManagementOrderDetailResult>.Fail("Order not found.", 404);
            }

            fromStatus = order.Status;

            if (order.PaymentStatus == PaymentStatus.Paid)
            {
                return ApiResult<ManagementOrderDetailResult>.Fail(
                    "Paid orders cannot be cancelled directly. Please flag them as RefundRequired instead.",
                    409);
            }

            if (order.Status == OrderStatus.Completed || order.Status == OrderStatus.Cancelled)
            {
                return ApiResult<ManagementOrderDetailResult>.Fail(
                    $"Cannot cancel an order in state '{order.Status}'.",
                    409);
            }

            var now = DateTimeOffset.UtcNow;
            var reason = string.IsNullOrWhiteSpace(command.Reason) ? null : command.Reason.Trim();

            order.Cancel(now, reason);
            order.MarkPaymentCancelled();
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

            var orderResult = ManagementOrderResultMapper.ToDetail(order);
            statusChangedEvent = CreateStatusChangedEvent(order, orderResult, fromStatus);
            return ApiResult<ManagementOrderDetailResult>.Success(
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
        ManagementOrderDetailResult result,
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
