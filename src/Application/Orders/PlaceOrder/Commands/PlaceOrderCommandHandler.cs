using Application.Abstractions.Realtime;
using Application.Abstractions.Realtime.Events;
using Application.Orders.Abstractions;
using Application.Orders.PlaceOrder.Mapping;
using Application.Orders.PlaceOrder.Results;
using Application.Orders.PlaceOrder.Requests;
using Application.Orders.PlaceOrder.Rules;
using Application.Orders.PlaceOrder.Support;
using Application.Orders.PlaceOrder.Services;
using Application.Shared.Wrappers;
using Application.Shared.Idempotency;
using Application.Tenants.Kiosks.Rules;
using Application.Tenants.Stores;
using Domain.Orders.Entities;

namespace Application.Orders.PlaceOrder.Commands;

public sealed class PlaceOrderCommandHandler
{
    private const string DefaultCurrency = "VND";
    private readonly IOrderStore _orderStore;
    private readonly IRealtimeNotificationPublisher _publisher;
    private readonly PlaceOrderItemAppender _itemAppender;

    public PlaceOrderCommandHandler(
        IOrderStore orderStore,
        IRealtimeNotificationPublisher publisher,
        PlaceOrderItemAppender itemAppender)
    {
        _orderStore = orderStore;
        _publisher = publisher;
        _itemAppender = itemAppender;
    }

    public async Task<ApiResult<OrderResult>> HandleAsync(
        PlaceOrderCommand command,
        CancellationToken cancellationToken = default)
    {
        var request = command.Request;

        var validationError = PlaceOrderRequestValidator.Validate(request);
        if (validationError is not null)
        {
            return ApiResult<OrderResult>.Fail(validationError);
        }

        if (!ScopedIdempotencyKey.TryNormalize(command.IdempotencyKey, out var idempotencyKey))
        {
            return ApiResult<OrderResult>.Fail(
                $"Idempotency-Key is required and must be at most {ScopedIdempotencyKey.MaxClientKeyLength} characters.",
                400);
        }
        var scopedIdempotencyKey = ScopedIdempotencyKey.ForKiosk(request.KioskId, idempotencyKey);
        var clientOrderId = NormalizeOptional(request.ClientOrderId);
        var clientOrderLockKey = clientOrderId is null
            ? null
            : $"client-order:{request.KioskId:N}:{clientOrderId}";

        OrderStatusChangedEvent? statusChangedEvent = null;
        var result = await _orderStore.ExecuteInTransactionAsync(async ct =>
        {
            foreach (var lockKey in new[] { scopedIdempotencyKey, clientOrderLockKey }
                .Where(lockKey => lockKey is not null)
                .Cast<string>()
                .Distinct(StringComparer.Ordinal)
                .OrderBy(lockKey => lockKey, StringComparer.Ordinal))
            {
                await _orderStore.AcquireIdempotencyLockAsync(lockKey, ct);
            }

            var existingByIdempotencyKey = await _orderStore.GetOrderByIdempotencyKeyAsync(scopedIdempotencyKey, ct);
            if (existingByIdempotencyKey is not null)
            {
                return IsEquivalentIdempotentRequest(existingByIdempotencyKey, request)
                    ? ApiResult<OrderResult>.Success(OrderResultMapper.ToResult(existingByIdempotencyKey), "Order already created.")
                    : ApiResult<OrderResult>.Fail("Idempotency key was already used for a different order request.", 409);
            }

            if (clientOrderId is not null)
            {
                var existingByClientOrderId = await _orderStore.GetOrderByClientOrderIdAsync(request.KioskId, clientOrderId, ct);
                if (existingByClientOrderId is not null)
                {
                    return IsEquivalentIdempotentRequest(existingByClientOrderId, request)
                        ? ApiResult<OrderResult>.Success(OrderResultMapper.ToResult(existingByClientOrderId), "Order already created.")
                        : ApiResult<OrderResult>.Fail("Client order id was already used for a different order request.", 409);
                }
            }

            var kiosk = await _orderStore.GetKioskByIdAsync(request.KioskId, ct);
            if (kiosk is null)
            {
                return ApiResult<OrderResult>.Fail("Kiosk not found.", 404);
            }

            var connectivity = await _orderStore.GetKioskConnectivityAsync(kiosk.Id, ct);
            var salesAvailabilityError = KioskSalesAvailabilityRules.ValidateOnlineSalesAvailability(kiosk, connectivity);
            if (salesAvailabilityError is not null)
            {
                return ApiResult<OrderResult>.Fail(salesAvailabilityError, 409);
            }

            var now = DateTimeOffset.UtcNow;
            var openingHoursError = StoreSalesAvailabilityRules.ValidateOpeningHours(kiosk.Store, now);
            if (openingHoursError is not null)
            {
                return ApiResult<OrderResult>.Fail(openingHoursError, 409);
            }

            var order = new Order
            {
                OrganizationId = kiosk.OrganizationId,
                StoreId = kiosk.StoreId,
                KioskId = kiosk.Id,
                OrderNumber = OrderNumberGenerator.GenerateOrderNumber(now),
                IdempotencyKey = scopedIdempotencyKey,
                ClientOrderId = clientOrderId,
                Channel = Domain.Orders.Enums.OrderChannel.Tablet,
                CustomerName = NormalizeOptional(request.CustomerName),
                CustomerPhoneNumber = NormalizeOptional(request.CustomerPhoneNumber),
                Notes = NormalizeOptional(request.Notes),
                CreatedAt = now
            };
            order.SetCurrency(DefaultCurrency);

            foreach (var itemRequest in request.Items)
            {
                var itemFailure = await _itemAppender.AppendAsync(order, kiosk, itemRequest, now, ct);
                if (itemFailure is not null)
                    return ApiResult<OrderResult>.Fail(itemFailure.Message, itemFailure.StatusCode);
            }

            order.Place(now);

            if (request.ClientTotalAmount.HasValue && request.ClientTotalAmount.Value != order.TotalAmount)
            {
                return ApiResult<OrderResult>.Fail("Client total does not match calculated total.", 409)
                    .AddDetail("clientTotalAmount", request.ClientTotalAmount.Value)
                    .AddDetail("calculatedTotalAmount", order.TotalAmount);
            }

            await _orderStore.AddOrderAsync(order, ct);

            var history = new OrderStatusHistory
            {
                Id = Guid.NewGuid(),
                OrderId = order.Id,
                FromStatus = Domain.Orders.Enums.OrderStatus.Draft,
                ToStatus = Domain.Orders.Enums.OrderStatus.PendingPayment,
                ChangedAt = now,
                Reason = "Order placed by customer."
            };
            await _orderStore.AddOrderStatusHistoryAsync(history, ct);

            await _orderStore.SaveChangesAsync(ct);

            var orderResult = OrderResultMapper.ToResult(order);
            statusChangedEvent = new OrderStatusChangedEvent
            {
                OrderId = order.Id,
                OrderNumber = order.OrderNumber,
                KioskId = order.KioskId,
                OrganizationId = order.OrganizationId,
                StoreId = order.StoreId,
                OldStatus = "None",
                NewStatus = orderResult.Status.ToString(),
                PaymentStatus = orderResult.PaymentStatus.ToString(),
                CustomerStatus = orderResult.CustomerStatus,
                CustomerStatusMessage = orderResult.CustomerStatusMessage,
                CanRetryPayment = orderResult.CanRetryPayment,
                RequiresStaffSupport = orderResult.RequiresStaffSupport,
                UpdatedAt = orderResult.PlacedAt,
                Version = 1
            };

            return ApiResult<OrderResult>.Success(orderResult, "Order created.", 201);
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

    private static bool IsEquivalentIdempotentRequest(Order order, PlaceOrderRequest request)
    {
        if (order.KioskId != request.KioskId ||
            !string.Equals(order.ClientOrderId, NormalizeOptional(request.ClientOrderId), StringComparison.Ordinal) ||
            !string.Equals(order.CustomerName, NormalizeOptional(request.CustomerName), StringComparison.Ordinal) ||
            !string.Equals(order.CustomerPhoneNumber, NormalizeOptional(request.CustomerPhoneNumber), StringComparison.Ordinal) ||
            !string.Equals(order.Notes, NormalizeOptional(request.Notes), StringComparison.Ordinal) ||
            (request.ClientTotalAmount.HasValue && request.ClientTotalAmount.Value != order.TotalAmount))
        {
            return false;
        }

        var requestedItems = request.Items
            .Select(item => string.Join('|',
                item.MenuItemId.ToString("N"),
                item.Quantity,
                NormalizeOptional(item.ClientLineId) ?? string.Empty,
                string.Join(',', item.SelectedOptions
                    .Select(option => option.ProductOptionId)
                    .OrderBy(option => option)
                    .Select(option => option.ToString("N")))))
            .OrderBy(value => value, StringComparer.Ordinal);
        var existingItems = order.OrderItems
            .Select(item => string.Join('|',
                item.MenuItemId.ToString("N"),
                item.Quantity,
                NormalizeOptional(item.ClientLineId) ?? string.Empty,
                string.Join(',', item.Options
                    .Select(option => option.ProductOptionId)
                    .OrderBy(option => option)
                    .Select(option => option.ToString("N")))))
            .OrderBy(value => value, StringComparer.Ordinal);

        return requestedItems.SequenceEqual(existingItems, StringComparer.Ordinal);
    }
}
