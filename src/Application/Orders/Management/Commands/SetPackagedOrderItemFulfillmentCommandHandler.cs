using Application.Abstractions.Realtime;
using Application.Abstractions.Realtime.Events;
using Application.Orders.Abstractions;
using Application.Orders.Management.Mapping;
using Application.Orders.Management.Results;
using Application.Orders.Support;
using Application.Shared.Wrappers;
using Application.Tenants;
using Domain.Catalog.Enums;
using Domain.Common;
using Domain.Orders.Entities;
using Domain.Orders.Enums;

namespace Application.Orders.Management.Commands;

public sealed class SetPackagedOrderItemFulfillmentCommandHandler(
    IOrderStore orders,
    IRealtimeNotificationPublisher publisher)
{
    public async Task<ApiResult<ManagementOrderDetailResult>> HandleAsync(
        SetPackagedOrderItemFulfillmentCommand command,
        CancellationToken cancellationToken = default)
    {
        if (command.OrderId == Guid.Empty || command.OrderItemId == Guid.Empty || command.FulfillmentEventId == Guid.Empty ||
            command.UserContext.AccountId == Guid.Empty)
            return ApiResult<ManagementOrderDetailResult>.Fail("Order, item, actor, and fulfillment event are required.");
        if (!Enum.IsDefined(command.Action))
            return ApiResult<ManagementOrderDetailResult>.Fail("Packaged fulfillment action is invalid.");
        if (command.Action == PackagedOrderItemFulfillmentAction.Fail && string.IsNullOrWhiteSpace(command.Reason))
            return ApiResult<ManagementOrderDetailResult>.Fail("A reason is required when packaged fulfillment fails.");
        var reason = string.IsNullOrWhiteSpace(command.Reason) ? null : command.Reason.Trim();
        if (reason?.Length > 500)
            return ApiResult<ManagementOrderDetailResult>.Fail("Fulfillment reason must not exceed 500 characters.");
        var payloadHash = OrderItemFulfillmentIdempotency.ComputePayloadHash(command.Action.ToString(), reason);

        OrderStatus? previousOrderStatus = null;
        Order? changedOrder = null;
        OrderItemFulfillmentChangedEvent? itemChangedEvent = null;
        var result = await orders.ExecuteInTransactionAsync(async ct =>
        {
            await orders.AcquireOrderWorkflowLockAsync(command.OrderId, ct);
            var order = await orders.GetOrderByIdAsync(command.OrderId, ct);
            if (order is null) return ApiResult<ManagementOrderDetailResult>.Fail("Order not found.", 404);
            if (!ScopeAccessRules.CanAccessScopedRow(
                    ScopeRoleSets.OrdersManage, command.UserContext,
                    order.OrganizationId, order.StoreId, order.KioskId))
                return ApiResult<ManagementOrderDetailResult>.Fail("Access denied.", 403);
            var item = order.OrderItems.SingleOrDefault(candidate => candidate.Id == command.OrderItemId);
            if (item is null) return ApiResult<ManagementOrderDetailResult>.Fail("Order item not found.", 404);
            if (item.FulfillmentType != FulfillmentType.Packaged)
                return ApiResult<ManagementOrderDetailResult>.Fail(
                    "Packaged fulfillment applies only to packaged order items.", 409);

            await orders.AcquireFulfillmentEventLockAsync(item.Id, command.FulfillmentEventId, ct);
            var existing = await orders.GetOrderItemStatusHistoryBySourceEventIdAsync(
                item.Id, command.FulfillmentEventId, ct);
            if (existing is not null)
            {
                if (!string.Equals(existing.SourcePayloadHash, payloadHash, StringComparison.Ordinal))
                    return ApiResult<ManagementOrderDetailResult>.Fail(
                        "Fulfillment event id was already used with a different payload.", 409);
                return ApiResult<ManagementOrderDetailResult>.Success(
                    ManagementOrderResultMapper.ToDetail(order),
                    "Existing fulfillment event returned for idempotent retry.");
            }

            if (order.PaymentStatus != PaymentStatus.Paid ||
                order.Status is OrderStatus.Cancelled or OrderStatus.Completed or OrderStatus.Refunded or
                    OrderStatus.Compensated or OrderStatus.RefundRequired)
                return ApiResult<ManagementOrderDetailResult>.Fail(
                    "A packaged item can be fulfilled only after payment and before the order becomes terminal.", 409);

            previousOrderStatus = order.Status;
            var changedAt = DateTimeOffset.UtcNow;
            var previousItemStatus = item.Status;
            bool itemChanged;
            try
            {
                itemChanged = command.Action switch
                {
                    PackagedOrderItemFulfillmentAction.Fulfill => item.FulfillPackaged(),
                    PackagedOrderItemFulfillmentAction.Fail => item.FailPackaged(reason!),
                    _ => throw new DomainRuleException("Packaged fulfillment action is invalid.")
                };
                if (itemChanged) OrderFulfillmentAggregator.Apply(order, changedAt, reason);
            }
            catch (DomainRuleException ex)
            {
                return ApiResult<ManagementOrderDetailResult>.Fail(ex.Message, 409);
            }

            if (!itemChanged)
                return ApiResult<ManagementOrderDetailResult>.Success(
                    ManagementOrderResultMapper.ToDetail(order),
                    command.Action == PackagedOrderItemFulfillmentAction.Fulfill
                        ? "Packaged order item was already fulfilled."
                        : "Packaged order item was already failed.");

            await orders.AddOrderItemStatusHistoryAsync(new OrderItemStatusHistory
            {
                OrderItemId = item.Id,
                SourceEventId = command.FulfillmentEventId,
                SourcePayloadHash = payloadHash,
                FromStatus = previousItemStatus,
                ToStatus = item.Status,
                ChangedAt = changedAt,
                ChangedByAccountId = command.UserContext.AccountId,
                Reason = command.Action == PackagedOrderItemFulfillmentAction.Fulfill
                    ? "Packaged item handed to the customer."
                    : reason
            }, ct);

            if (order.Status != previousOrderStatus)
                await orders.AddOrderStatusHistoryAsync(new OrderStatusHistory
                {
                    OrderId = order.Id,
                    FromStatus = previousOrderStatus.Value,
                    ToStatus = order.Status,
                    ChangedAt = changedAt,
                    ChangedByAccountId = command.UserContext.AccountId,
                    Reason = command.Action == PackagedOrderItemFulfillmentAction.Fulfill
                        ? "Packaged order item fulfilled."
                        : $"Packaged order item failed: {reason}"
                }, ct);

            order.UpdatedAt = changedAt;
            order.UpdatedByAccountId = command.UserContext.AccountId;
            await orders.SaveChangesAsync(ct);
            changedOrder = order;
            itemChangedEvent = BuildItemChangedEvent(order, item, previousItemStatus, changedAt);
            return ApiResult<ManagementOrderDetailResult>.Success(
                ManagementOrderResultMapper.ToDetail(order),
                command.Action == PackagedOrderItemFulfillmentAction.Fulfill
                    ? "Packaged order item fulfilled."
                    : "Packaged order item marked as failed.");
        }, cancellationToken);

        if (result.Succeeded && changedOrder is not null && previousOrderStatus != changedOrder.Status)
            await PublishStatusChangedAsync(changedOrder, previousOrderStatus!.Value, cancellationToken);
        if (result.Succeeded && itemChangedEvent is not null)
            await publisher.PublishOrderItemFulfillmentChangedAsync(itemChangedEvent, cancellationToken);
        return result;
    }

    private static OrderItemFulfillmentChangedEvent BuildItemChangedEvent(
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

    private Task PublishStatusChangedAsync(Order order, OrderStatus oldStatus, CancellationToken cancellationToken)
    {
        var projection = OrderStatusProjector.ProjectFromOrder(order);
        return publisher.PublishOrderStatusChangedAsync(new OrderStatusChangedEvent
        {
            OrderId = order.Id, OrderNumber = order.OrderNumber, KioskId = order.KioskId,
            OrganizationId = order.OrganizationId, StoreId = order.StoreId,
            OldStatus = oldStatus.ToString(), NewStatus = order.Status.ToString(),
            PaymentStatus = order.PaymentStatus.ToString(), CustomerStatus = projection.CustomerStatus,
            CustomerStatusMessage = projection.CustomerStatusMessage, CanRetryPayment = projection.CanRetryPayment,
            RequiresStaffSupport = projection.RequiresStaffSupport,
            UpdatedAt = order.UpdatedAt ?? DateTimeOffset.UtcNow, Version = 1
        }, cancellationToken);
    }
}
