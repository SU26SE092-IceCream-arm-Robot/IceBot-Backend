using Application.Orders.Abstractions;
using Application.Orders.PlaceOrder.Mapping;
using Application.Orders.PlaceOrder.Results;
using Application.Orders.PlaceOrder.Rules;
using Application.Orders.PlaceOrder.Support;
using Application.Shared.Wrappers;
using Application.Tenants.Kiosks.Rules;
using Domain.Orders.Entities;
using Domain.SalesCatalog.Enums;

using Application.Abstractions.Realtime;
using Application.Abstractions.Realtime.Events;

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

        var idempotencyKey = NormalizeOptional(request.IdempotencyKey);
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
                CorrelationId = request.RuntimeSnapshotId,
                RuntimeSnapshotId = request.RuntimeSnapshotId,
                RuntimeSnapshotGeneratedAt = request.RuntimeSnapshotGeneratedAt,
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

                if (recipe is not null)
                {
                    var recipeValidationError = RecipeValidationRules.ValidateRecipe(recipe, productVariant, kiosk.OrganizationId, kiosk.StoreId, kiosk.Id);
                    if (recipeValidationError is not null)
                    {
                        return ApiResult<OrderResult>.Fail(recipeValidationError, 409);
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

            return ApiResult<OrderResult>.Success(OrderResultMapper.ToResult(order), "Order created.", 201);
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
                OldStatus = "None",
                NewStatus = result.Data.Status.ToString(),
                PaymentStatus = result.Data.PaymentStatus.ToString(),
                CustomerStatus = result.Data.CustomerStatus,
                CustomerStatusMessage = result.Data.CustomerStatusMessage,
                CanRetryPayment = result.Data.CanRetryPayment,
                RequiresStaffSupport = result.Data.RequiresStaffSupport,
                UpdatedAt = result.Data.PlacedAt,
                Version = 1
            }, cancellationToken);
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
