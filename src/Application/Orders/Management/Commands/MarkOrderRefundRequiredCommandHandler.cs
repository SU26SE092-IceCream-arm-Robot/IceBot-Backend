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

public sealed class MarkOrderRefundRequiredCommandHandler
{
    private readonly IOrderStore _orderStore;
    private readonly IRealtimeNotificationPublisher _publisher;

    public MarkOrderRefundRequiredCommandHandler(IOrderStore orderStore, IRealtimeNotificationPublisher publisher)
    {
        _orderStore = orderStore;
        _publisher = publisher;
    }

    public async Task<ApiResult<ManagementOrderDetailResult>> HandleAsync(
        MarkOrderRefundRequiredCommand command,
        CancellationToken cancellationToken = default)
    {
        var reason = command.Reason?.Trim();
        if (string.IsNullOrWhiteSpace(reason))
        {
            return ApiResult<ManagementOrderDetailResult>.Fail("Reason is required to flag an order as refund required.", 400);
        }

        OrderStatus fromStatus = OrderStatus.Draft;
        OrderStatusChangedEvent? statusChangedEvent = null;

        var result = await _orderStore.ExecuteInTransactionAsync(async ct =>
        {
            await _orderStore.AcquireOrderWorkflowLockAsync(command.OrderId, ct);
            var order = await _orderStore.GetOrderByIdAsync(command.OrderId, ct);
            if (order is null)
            {
                return ApiResult<ManagementOrderDetailResult>.Fail("Order not found.", 404);
            }

            fromStatus = order.Status;

            if (!ScopeAccessRules.CanAccessScopedRow(ScopeRoleSets.OrdersManage,
                command.UserContext,
                order.OrganizationId,
                order.StoreId,
                order.KioskId))
            {
                return ApiResult<ManagementOrderDetailResult>.Fail("Access denied.", 403);
            }

            if (order.PaymentStatus != PaymentStatus.Paid)
            {
                return ApiResult<ManagementOrderDetailResult>.Fail(
                    "Only paid orders can require refund. For unpaid orders, please cancel them directly.",
                    409);
            }

            if (order.Status == OrderStatus.Completed || order.Status == OrderStatus.Cancelled)
            {
                return ApiResult<ManagementOrderDetailResult>.Fail(
                    $"Cannot flag completed or cancelled order as refund required.",
                    409);
            }

            var now = DateTimeOffset.UtcNow;

            order.MarkRefundRequired(reason);
            order.UpdatedAt = now;

            var history = new OrderStatusHistory
            {
                Id = Guid.NewGuid(),
                OrderId = order.Id,
                FromStatus = fromStatus,
                ToStatus = OrderStatus.RefundRequired,
                ChangedByAccountId = command.UserContext.AccountId,
                Reason = reason,
                ChangedAt = now
            };
            await _orderStore.AddOrderStatusHistoryAsync(history, ct);

            await _orderStore.SaveChangesAsync(ct);

            var orderResult = ManagementOrderResultMapper.ToDetail(order);
            statusChangedEvent = CreateStatusChangedEvent(order, orderResult, fromStatus);
            return ApiResult<ManagementOrderDetailResult>.Success(
                orderResult,
                "Order flagged as refund required successfully.");
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
        UpdatedAt = DateTimeOffset.UtcNow,
        Version = 1
    };
}
