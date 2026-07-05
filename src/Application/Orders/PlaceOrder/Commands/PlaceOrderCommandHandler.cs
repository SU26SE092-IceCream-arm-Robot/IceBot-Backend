using Application.Abstractions.Realtime;
using Application.Abstractions.Realtime.Events;
using Application.Orders.Abstractions;
using Application.Orders.PlaceOrder.Mapping;
using Application.Orders.PlaceOrder.Results;
using Application.Orders.PlaceOrder.Rules;
using Application.Orders.PlaceOrder.Support;
using Application.Shared.Wrappers;
using Application.Tenants.Kiosks.Rules;
using Domain.Catalog.Enums;
using Domain.Orders.Entities;
using Domain.SalesCatalog.Enums;

namespace Application.Orders.PlaceOrder.Commands;

public sealed class PlaceOrderCommandHandler
{
    private const string DefaultCurrency = "VND";
    private readonly IOrderStore _orderStore;
    private readonly IRealtimeNotificationPublisher _publisher;

    public PlaceOrderCommandHandler(IOrderStore orderStore, IRealtimeNotificationPublisher publisher)
    {
        _orderStore = orderStore;
        _publisher = publisher;
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

        var idempotencyKey = NormalizeOptional(command.IdempotencyKey);
        if (idempotencyKey is not null)
        {
            var existing = await _orderStore.GetOrderByIdempotencyKeyAsync(idempotencyKey, cancellationToken);
            if (existing is not null)
            {
                return ApiResult<OrderResult>.Success(OrderResultMapper.ToResult(existing), "Order already created.");
            }
        }

        var clientOrderId = NormalizeOptional(request.ClientOrderId);
        if (clientOrderId is not null)
        {
            var existing = await _orderStore.GetOrderByClientOrderIdAsync(request.KioskId, clientOrderId, cancellationToken);
            if (existing is not null)
            {
                return ApiResult<OrderResult>.Success(OrderResultMapper.ToResult(existing), "Order already created.");
            }
        }

        OrderStatusChangedEvent? statusChangedEvent = null;
        var result = await _orderStore.ExecuteInTransactionAsync(async ct =>
        {
            var kiosk = await _orderStore.GetKioskByIdAsync(request.KioskId, ct);
            if (kiosk is null)
            {
                return ApiResult<OrderResult>.Fail("Kiosk not found.", 404);
            }

            var salesAvailabilityError = KioskSalesAvailabilityRules.ValidateOnlineSalesAvailability(kiosk);
            if (salesAvailabilityError is not null)
            {
                return ApiResult<OrderResult>.Fail(salesAvailabilityError, 409);
            }

            var now = DateTimeOffset.UtcNow;
            var order = new Order
            {
                OrganizationId = kiosk.OrganizationId,
                StoreId = kiosk.StoreId,
                KioskId = kiosk.Id,
                OrderNumber = OrderNumberGenerator.GenerateOrderNumber(now),
                IdempotencyKey = idempotencyKey,
                ClientOrderId = clientOrderId,
                Channel = request.Channel,
                Currency = DefaultCurrency,
                CustomerName = NormalizeOptional(request.CustomerName),
                CustomerPhoneNumber = NormalizeOptional(request.CustomerPhoneNumber),
                Notes = NormalizeOptional(request.Notes),
                CreatedAt = now
            };

            foreach (var itemRequest in request.Items)
            {
                var menuItem = await _orderStore.GetMenuItemByIdAsync(itemRequest.MenuItemId, ct);
                if (menuItem is null)
                {
                    return ApiResult<OrderResult>.Fail($"Menu item '{itemRequest.MenuItemId}' not found.", 404);
                }

                var menu = menuItem.Menu;
                var product = menuItem.Product;
                var productVariant = menuItem.ProductVariant;
                var recipe = menuItem.Recipe;

                if (menu.Status != MenuStatus.Active)
                {
                    return ApiResult<OrderResult>.Fail($"Menu '{menu.Name}' is not active.", 409);
                }

                if (!PlaceOrderScopeRules.IsWithinEffectiveWindow(menu.EffectiveFrom, menu.EffectiveTo, now))
                {
                    return ApiResult<OrderResult>.Fail($"Menu '{menu.Name}' is not active at this time.", 409);
                }

                if (!menuItem.IsCurrentlySellable(now))
                {
                    return ApiResult<OrderResult>.Fail($"Menu item '{menuItem.DisplayName}' is not available.", 409);
                }

                if (!product.IsAvailable)
                {
                    return ApiResult<OrderResult>.Fail($"Product '{product.Name}' is not available.", 409);
                }

                if (!productVariant.IsAvailable)
                {
                    return ApiResult<OrderResult>.Fail($"Product variant '{productVariant.Name}' is not available.", 409);
                }

                if (productVariant.ProductId != product.Id)
                {
                    return ApiResult<OrderResult>.Fail("Menu item variant does not belong to the selected product.", 409);
                }

                if (!order.OrderItems.Any())
                {
                    order.Currency = menuItem.Currency;
                }
                else if (!CurrencyMatches(order.Currency, menuItem.Currency))
                {
                    return ApiResult<OrderResult>.Fail("All order items must use the same currency.", 400);
                }

                if (!PlaceOrderScopeRules.MatchesScope(menu.OrganizationId, kiosk.OrganizationId) ||
                    !PlaceOrderScopeRules.MatchesScope(menu.StoreId, kiosk.StoreId) ||
                    !PlaceOrderScopeRules.MatchesScope(menu.KioskId, kiosk.Id))
                {
                    return ApiResult<OrderResult>.Fail($"Menu '{menu.Name}' is not available for this kiosk.", 409);
                }

                if (!PlaceOrderScopeRules.MatchesScope(product.OrganizationId, kiosk.OrganizationId) ||
                    !PlaceOrderScopeRules.MatchesScope(product.StoreId, kiosk.StoreId) ||
                    !PlaceOrderScopeRules.MatchesScope(product.KioskId, kiosk.Id))
                {
                    return ApiResult<OrderResult>.Fail($"Product '{product.Name}' is not available for this kiosk.", 409);
                }

                if (productVariant.FulfillmentType == FulfillmentType.MachineProduced && recipe is null)
                {
                    return ApiResult<OrderResult>.Fail($"Menu item '{menuItem.DisplayName}' requires a recipe.", 409);
                }

                if (recipe is not null)
                {
                    var recipeValidationError = RecipeValidationRules.ValidateRecipe(recipe, productVariant, kiosk.OrganizationId, kiosk.StoreId, kiosk.Id, now);
                    if (recipeValidationError is not null)
                    {
                        return ApiResult<OrderResult>.Fail(recipeValidationError, 409);
                    }
                }

                if (productVariant.FulfillmentType == FulfillmentType.MachineProduced)
                {
                    var hasActiveProductionRoute = await _orderStore.HasActiveProductionRouteAsync(kiosk.Id, productVariant.Id, recipe!.Id, ct);
                    if (!hasActiveProductionRoute)
                    {
                        return ApiResult<OrderResult>.Fail($"Menu item '{menuItem.DisplayName}' does not have an active production route for this kiosk.", 409);
                    }
                }

                var orderItem = order.AddItem(
                    menuItem.Id,
                    product.Id,
                    productVariant.Id,
                    recipe?.Id,
                    menuItem.Code,
                    menuItem.DisplayName,
                    product.Code,
                    product.DisplayName ?? product.Name,
                    productVariant.Code,
                    productVariant.DisplayName ?? productVariant.Name,
                    recipe?.Version,
                    itemRequest.Quantity,
                    menuItem.Price,
                    menuItem.DiscountAmount,
                    NormalizeOptional(itemRequest.ClientLineId),
                    optionsJson: itemRequest.OptionsJson,
                    recipeSnapshotJson: recipe is null ? null : RecipeSnapshotBuilder.BuildRecipeSnapshotJson(recipe));

                orderItem.CreatedAt = now;
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

    private static bool CurrencyMatches(string orderCurrency, string productCurrency)
    {
        return string.Equals(orderCurrency, productCurrency, StringComparison.OrdinalIgnoreCase);
    }

    private static string? NormalizeOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
