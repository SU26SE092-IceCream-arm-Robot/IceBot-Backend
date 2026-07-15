using Application.Abstractions.Realtime;
using Application.Abstractions.Realtime.Events;
using Application.Orders.Abstractions;
using Application.Orders.PlaceOrder.Mapping;
using Application.Orders.PlaceOrder.Results;
using Application.Orders.PlaceOrder.Requests;
using Application.Orders.PlaceOrder.Rules;
using Application.Orders.PlaceOrder.Support;
using Application.Shared.Wrappers;
using Application.Shared.Idempotency;
using Application.Tenants.Kiosks.Rules;
using Application.Tenants.Stores;
using Domain.Catalog.Enums;
using Domain.Orders.Entities;
using Domain.SalesCatalog.Enums;
using Application.SalesCatalog.Rules;

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
                    order.SetCurrency(menuItem.Currency);
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

                var selectableOptions = await _orderStore.ListMenuItemProductOptionsAsync(menuItem.Id, ct);
                var selectedOptionIds = itemRequest.SelectedOptions
                    .Select(option => option.ProductOptionId)
                    .ToArray();
                var optionValidationError = ProductOptionSelectionRules.Validate(
                    selectableOptions,
                    selectedOptionIds);
                if (optionValidationError is not null)
                {
                    return ApiResult<OrderResult>.Fail(optionValidationError, 409);
                }

                var selectedOptions = selectableOptions
                    .Where(option => selectedOptionIds.Contains(option.ProductOptionId))
                    .OrderBy(option => option.OptionGroupId)
                    .ThenBy(option => option.DisplayOrder)
                    .ToArray();
                if (productVariant.FulfillmentType == FulfillmentType.Packaged && selectedOptions.Any(option =>
                        option.ExecutionImpact == ProductOptionExecutionImpact.ProductionAffecting))
                {
                    return ApiResult<OrderResult>.Fail(
                        $"Packaged menu item '{menuItem.DisplayName}' cannot use production-affecting options. Use a product variant for physical packaged choices.",
                        409);
                }
                if (productVariant.FulfillmentType == FulfillmentType.MachineProduced)
                {
                    var routePolicy = await _orderStore.GetActiveProductionRouteOptionPolicyAsync(
                        kiosk.Id, productVariant.Id, recipe!.Id, ct);
                    if (routePolicy is null)
                    {
                        return ApiResult<OrderResult>.Fail(
                            $"Menu item '{menuItem.DisplayName}' does not have an active production route for this kiosk.", 409);
                    }

                    var unsupportedOptions = selectedOptions.Where(option =>
                        option.ExecutionImpact == ProductOptionExecutionImpact.ProductionAffecting &&
                        !routePolicy.SupportedOptionCodes.Contains(option.Code)).ToArray();
                    if (unsupportedOptions.Length > 0)
                    {
                        return ApiResult<OrderResult>.Fail(
                            $"One or more selected options are not supported by the active production route for '{menuItem.DisplayName}'.", 409);
                    }
                }
                var optionIngredientRequirements = await _orderStore.ListProductOptionIngredientRequirementsAsync(
                    selectedOptions.Select(option => option.ProductOptionId).ToArray(), ct);
                if (optionIngredientRequirements.Any(requirement => !requirement.IsIngredientActive))
                {
                    return ApiResult<OrderResult>.Fail("One or more selected options require an inactive ingredient.", 409);
                }
                var optionUnitPriceDelta = selectedOptions.Sum(option => option.PriceDelta);

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
                    productVariant.FulfillmentType,
                    itemRequest.Quantity,
                    menuItem.Price + optionUnitPriceDelta,
                    menuItem.DiscountAmount,
                    NormalizeOptional(itemRequest.ClientLineId),
                    recipeSnapshotJson: recipe is null ? null : RecipeSnapshotBuilder.BuildRecipeSnapshotJson(recipe));

                orderItem.CreatedAt = now;
                foreach (var selectedOption in selectedOptions)
                {
                    var snapshot = OrderItemOption.Create(
                        selectedOption.ProductOptionId,
                        selectedOption.OptionGroupId,
                        selectedOption.OptionGroupCode,
                        selectedOption.Code,
                        selectedOption.Name,
                        selectedOption.PriceDelta,
                        selectedOption.ExecutionImpact);
                    snapshot.OrderItemId = orderItem.Id;
                    snapshot.CreatedAt = now;
                    foreach (var requirement in optionIngredientRequirements
                                 .Where(requirement => requirement.ProductOptionId == selectedOption.ProductOptionId))
                    {
                        snapshot.IngredientRequirements.Add(new OrderItemOptionIngredientRequirement
                        {
                            OrderItemOptionId = snapshot.Id,
                            IngredientId = requirement.IngredientId,
                            IngredientCodeSnapshot = requirement.IngredientCode,
                            IngredientNameSnapshot = requirement.IngredientName,
                            QuantityPerOption = requirement.Quantity,
                            Unit = requirement.Unit,
                            RequiredWorkcellCapabilityCode = requirement.RequiredWorkcellCapabilityCode,
                            CreatedAt = now
                        });
                    }
                    orderItem.Options.Add(snapshot);
                }
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
