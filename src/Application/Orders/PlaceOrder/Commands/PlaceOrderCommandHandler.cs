using Application.Abstractions.Realtime;
using Application.ClientDevices;
using Application.Abstractions.Realtime.Events;
using Application.Orders.Abstractions;
using Application.Orders.PlaceOrder;
using Application.Orders.PlaceOrder.Mapping;
using Application.Orders.PlaceOrder.Results;
using Application.Orders.PlaceOrder.Requests;
using Application.Orders.PlaceOrder.Rules;
using Application.Orders.PlaceOrder.Support;
using Application.Orders.PlaceOrder.Services;
using Application.Shared.Wrappers;
using Application.Shared.Idempotency;
using Application.SalesCatalog.Admission;
using Application.SalesCatalog.Admission.Services;
using Domain.Orders.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Application.Orders.PlaceOrder.Commands;

public sealed class PlaceOrderCommandHandler
{
    private const string DefaultCurrency = "VND";
    private readonly IOrderStore _orderStore;
    private readonly IRealtimeNotificationPublisher _publisher;
    private readonly PlaceOrderItemAppender _itemAppender;
    private readonly OrderPaymentWindowOptions _paymentWindow;
    private readonly KioskSalesAdmissionEvaluator _admissionEvaluator;
    private readonly ClientDeviceRuntimeOptions _runtimeLimits;

    public PlaceOrderCommandHandler(
        IOrderStore orderStore,
        IRealtimeNotificationPublisher publisher,
        PlaceOrderItemAppender itemAppender,
        IOptions<OrderPaymentWindowOptions> paymentWindow,
        KioskSalesAdmissionEvaluator admissionEvaluator,
        IOptions<ClientDeviceRuntimeOptions> runtimeLimits)
    {
        _orderStore = orderStore;
        _publisher = publisher;
        _itemAppender = itemAppender;
        _paymentWindow = paymentWindow.Value;
        _admissionEvaluator = admissionEvaluator;
        _runtimeLimits = runtimeLimits.Value;
    }

    public async Task<ApiResult<OrderResult>> HandleAsync(
        PlaceOrderCommand command,
        CancellationToken cancellationToken = default)
    {
        var request = command.Request;

        if (command.KioskId == Guid.Empty || command.SourceClientDeviceId == Guid.Empty)
        {
            return ApiResult<OrderResult>.Fail("An authenticated client device scope is required.", 401);
        }

        var validationErrors = PlaceOrderRequestValidator.Validate(request, _runtimeLimits);
        if (validationErrors is not null)
        {
            return ApiResult<OrderResult>.ValidationFailure(validationErrors);
        }

        if (!ScopedIdempotencyKey.TryNormalize(command.IdempotencyKey, out var idempotencyKey))
        {
            return ApiResult<OrderResult>.BusinessFailure(
                OrderErrors.IdempotencyKeyInvalid);
        }
        var scopedIdempotencyKey = ScopedIdempotencyKey.ForClientDevice(command.SourceClientDeviceId, idempotencyKey);
        var clientOrderId = NormalizeOptional(request.ClientOrderId);
        var clientOrderLockKey = clientOrderId is null
            ? null
            : $"client-order:{command.KioskId:N}:{clientOrderId}";

        OrderStatusChangedEvent? statusChangedEvent = null;
        ApiResult<OrderResult> result;
        try
        {
            result = await _orderStore.ExecuteCheckoutTransactionAsync(async ct =>
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
                    return IsEquivalentIdempotentRequest(existingByIdempotencyKey, command, request)
                        ? ApiResult<OrderResult>.Success(OrderResultMapper.ToResult(existingByIdempotencyKey), "Order already created.")
                        : ApiResult<OrderResult>.BusinessFailure(
                            OrderErrors.IdempotencyConflict);
                }

                if (clientOrderId is not null)
                {
                    var existingByClientOrderId = await _orderStore.GetOrderByClientOrderIdAsync(command.KioskId, clientOrderId, ct);
                    if (existingByClientOrderId is not null)
                    {
                        return IsEquivalentIdempotentRequest(existingByClientOrderId, command, request)
                            ? ApiResult<OrderResult>.Success(OrderResultMapper.ToResult(existingByClientOrderId), "Order already created.")
                            : ApiResult<OrderResult>.BusinessFailure(
                                OrderErrors.ClientOrderIdConflict);
                    }
                }

                var kiosk = await _orderStore.GetKioskByIdAsync(command.KioskId, ct);
                if (kiosk is null)
                {
                    return ApiResult<OrderResult>.Fail("Kiosk not found.", 404);
                }

                await _orderStore.AcquireKioskOperationalLockAsync(kiosk.Id, ct);

                var now = DateTimeOffset.UtcNow;
                var admission = await _admissionEvaluator.EvaluateAsync(kiosk, new KioskSalesAdmissionRequest(now), ct);
                if (!admission.CanPlaceOrder)
                {
                    var blocker = admission.PrimaryBlocker
                        ?? throw new InvalidOperationException("Blocked kiosk admission must provide a blocker.");
                    return ApiResult<OrderResult>.BusinessFailure(
                        SalesAdmissionErrors.For(blocker.Code));
                }

                var order = new Order
                {
                    OrganizationId = kiosk.OrganizationId,
                    StoreId = kiosk.StoreId,
                    KioskId = kiosk.Id,
                    SourceClientDeviceId = command.SourceClientDeviceId,
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
                        return itemFailure.BusinessError is null
                            ? ApiResult<OrderResult>.Fail(itemFailure.Message, itemFailure.StatusCode)
                            : ApiResult<OrderResult>.BusinessFailure(itemFailure.BusinessError);
                }

                order.Place(now, now.AddMinutes(_paymentWindow.DurationMinutes));

                if (request.ClientTotalAmount.HasValue && request.ClientTotalAmount.Value != order.TotalAmount)
                {
                    return ApiResult<OrderResult>.BusinessFailure(
                            OrderErrors.ClientTotalMismatch)
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
        }
        catch (DbUpdateException)
        {
            statusChangedEvent = null;
            var replay = await ResolveConcurrentReplayAsync(
                scopedIdempotencyKey,
                command,
                request,
                clientOrderId,
                cancellationToken);
            if (replay is null)
                throw;

            result = replay;
        }

        if (result.Succeeded && statusChangedEvent is not null)
        {
            await _publisher.PublishOrderStatusChangedAsync(statusChangedEvent, cancellationToken);
        }

        return result;
    }

    private async Task<ApiResult<OrderResult>?> ResolveConcurrentReplayAsync(
        string scopedIdempotencyKey,
        PlaceOrderCommand command,
        PlaceOrderRequest request,
        string? clientOrderId,
        CancellationToken cancellationToken)
    {
        var existingByIdempotencyKey = await _orderStore.GetOrderByIdempotencyKeyAsync(
            scopedIdempotencyKey,
            cancellationToken);
        if (existingByIdempotencyKey is not null)
        {
            return IsEquivalentIdempotentRequest(existingByIdempotencyKey, command, request)
                ? ApiResult<OrderResult>.Success(OrderResultMapper.ToResult(existingByIdempotencyKey), "Order already created.")
                : ApiResult<OrderResult>.BusinessFailure(
                    OrderErrors.IdempotencyConflict);
        }

        if (clientOrderId is null)
            return null;

        var existingByClientOrderId = await _orderStore.GetOrderByClientOrderIdAsync(
            command.KioskId,
            clientOrderId,
            cancellationToken);
        if (existingByClientOrderId is null)
            return null;

        return IsEquivalentIdempotentRequest(existingByClientOrderId, command, request)
            ? ApiResult<OrderResult>.Success(OrderResultMapper.ToResult(existingByClientOrderId), "Order already created.")
            : ApiResult<OrderResult>.BusinessFailure(
                OrderErrors.ClientOrderIdConflict);
    }

    private static string? NormalizeOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private static bool IsEquivalentIdempotentRequest(Order order, PlaceOrderCommand command, PlaceOrderRequest request)
    {
        if (order.KioskId != command.KioskId ||
            order.SourceClientDeviceId != command.SourceClientDeviceId ||
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
