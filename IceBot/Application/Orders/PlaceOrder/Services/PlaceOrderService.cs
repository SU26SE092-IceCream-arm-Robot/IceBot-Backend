using Application.Orders.Abstractions;
using Application.Orders.PlaceOrder.Requests;
using Application.Orders.PlaceOrder.Results;
using Application.Shared.Wrappers;
using Domain.Catalog.Entities;
using Domain.Catalog.Enums;
using Domain.Common;
using Domain.Orders.Entities;
using Domain.Orders.Enums;
using Domain.SalesCatalog.Entities;
using Domain.SalesCatalog.Enums;
using Domain.Tenants.Enums;
using System.Text.Json;

namespace Application.Orders.PlaceOrder.Services;

public sealed class PlaceOrderService
{
    private const string DefaultCurrency = "VND";

    private readonly IOrderStore _orderStore;

    public PlaceOrderService(IOrderStore orderStore)
    {
        _orderStore = orderStore;
    }

    public async Task<ApiResult<OrderResult>> PlaceOrderAsync(
        PlaceOrderRequest request,
        CancellationToken cancellationToken = default)
    {
        var validationError = ValidatePlaceOrderRequest(request);
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
                return ApiResult<OrderResult>.Success(ToResult(existing), "Order already created.");
            }
        }

        var clientOrderId = NormalizeOptional(request.ClientOrderId);
        if (clientOrderId is not null)
        {
            var existing = await _orderStore.GetOrderByClientOrderIdAsync(request.KioskId, clientOrderId, cancellationToken);
            if (existing is not null)
            {
                return ApiResult<OrderResult>.Success(ToResult(existing), "Order already created.");
            }
        }

        return await _orderStore.ExecuteInTransactionAsync(async ct =>
        {
            var kiosk = await _orderStore.GetKioskByIdAsync(request.KioskId, ct);
            if (kiosk is null)
            {
                return ApiResult<OrderResult>.Fail("Kiosk not found.", 404);
            }

            if (kiosk.Status is KioskStatus.Disabled or KioskStatus.Retired)
            {
                return ApiResult<OrderResult>.Fail("Kiosk is not available for checkout.", 409);
            }

            var now = DateTimeOffset.UtcNow;
            var order = new Order
            {
                OrganizationId = kiosk.OrganizationId,
                StoreId = kiosk.StoreId,
                KioskId = kiosk.Id,
                OrderNumber = GenerateOrderNumber(now),
                IdempotencyKey = idempotencyKey,
                ClientOrderId = clientOrderId,
                CorrelationId = request.RuntimeSnapshotId,
                Channel = request.Channel,
                Currency = DefaultCurrency,
                CustomerName = NormalizeOptional(request.CustomerName),
                CustomerPhoneNumber = NormalizeOptional(request.CustomerPhoneNumber),
                Notes = BuildOrderNotes(request),
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

                if (!IsWithinEffectiveWindow(menu.EffectiveFrom, menu.EffectiveTo, now))
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

                if (!MatchesScope(menu.OrganizationId, kiosk.OrganizationId) ||
                    !MatchesScope(menu.StoreId, kiosk.StoreId) ||
                    !MatchesScope(menu.KioskId, kiosk.Id))
                {
                    return ApiResult<OrderResult>.Fail($"Menu '{menu.Name}' is not available for this kiosk.", 409);
                }

                if (!MatchesScope(product.OrganizationId, kiosk.OrganizationId) ||
                    !MatchesScope(product.StoreId, kiosk.StoreId) ||
                    !MatchesScope(product.KioskId, kiosk.Id))
                {
                    return ApiResult<OrderResult>.Fail($"Product '{product.Name}' is not available for this kiosk.", 409);
                }

                if (recipe is not null)
                {
                    var recipeValidationError = ValidateRecipe(recipe, productVariant, kiosk.OrganizationId, kiosk.StoreId, kiosk.Id);
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
                    product.Code,
                    product.DisplayName ?? product.Name,
                    productVariant.Code,
                    menuItem.DisplayName,
                    itemRequest.Quantity,
                    menuItem.Price,
                    menuItem.DiscountAmount,
                    NormalizeOptional(itemRequest.ClientLineId),
                    optionsJson: itemRequest.OptionsJson,
                    recipeSnapshotJson: recipe is null ? null : BuildRecipeSnapshotJson(recipe));

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
            await _orderStore.SaveChangesAsync(ct);

            return ApiResult<OrderResult>.Success(ToResult(order), "Order created.", 201);
        }, cancellationToken);
    }

    public async Task<ApiResult<OrderResult>> GetOrderStatusAsync(Guid orderId, CancellationToken cancellationToken = default)
    {
        var order = await _orderStore.GetOrderByIdAsync(orderId, cancellationToken);
        if (order is null)
        {
            return ApiResult<OrderResult>.Fail("Order not found.", 404);
        }

        return ApiResult<OrderResult>.Success(ToResult(order));
    }

    public async Task<ApiResult<OrderResult>> CancelPendingOrderAsync(
        Guid orderId,
        CancelPendingOrderRequest request,
        CancellationToken cancellationToken = default)
    {
        return await _orderStore.ExecuteInTransactionAsync(async ct =>
        {
            var order = await _orderStore.GetOrderByIdAsync(orderId, ct);
            if (order is null)
            {
                return ApiResult<OrderResult>.Fail("Order not found.", 404);
            }

            if (order.PaymentStatus == PaymentStatus.Paid)
            {
                return ApiResult<OrderResult>.Fail("Paid orders cannot be cancelled through this endpoint.", 409);
            }

            if (order.Status is not (OrderStatus.Draft or OrderStatus.PendingPayment))
            {
                return ApiResult<OrderResult>.Fail("Only draft or pending-payment orders can be cancelled.", 409);
            }

            order.Cancel(DateTimeOffset.UtcNow, NormalizeOptional(request.Reason));
            order.PaymentStatus = PaymentStatus.Cancelled;
            order.UpdatedAt = DateTimeOffset.UtcNow;
            await _orderStore.SaveChangesAsync(ct);

            return ApiResult<OrderResult>.Success(ToResult(order), "Order cancelled.");
        }, cancellationToken);
    }

    private static string? ValidatePlaceOrderRequest(PlaceOrderRequest request)
    {
        if (request.KioskId == Guid.Empty)
        {
            return "Kiosk is required.";
        }

        if (request.Items.Count == 0)
        {
            return "Order must contain at least one item.";
        }

        if (request.Items.Any(item => item.MenuItemId == Guid.Empty))
        {
            return "Menu item is required for every order item.";
        }

        if (request.Items.Any(item => item.Quantity <= 0))
        {
            return "Order item quantity must be greater than zero.";
        }

        var duplicateClientLineId = request.Items
            .Where(item => !string.IsNullOrWhiteSpace(item.ClientLineId))
            .GroupBy(item => item.ClientLineId!.Trim(), StringComparer.OrdinalIgnoreCase)
            .Any(group => group.Count() > 1);

        return duplicateClientLineId ? "Duplicate client line id in order items." : null;
    }

    private static OrderResult ToResult(Order order)
    {
        return new OrderResult
        {
            Id = order.Id,
            KioskId = order.KioskId,
            StoreId = order.StoreId,
            OrganizationId = order.OrganizationId,
            OrderNumber = order.OrderNumber,
            ClientOrderId = order.ClientOrderId,
            Channel = order.Channel,
            Status = order.Status,
            PaymentStatus = order.PaymentStatus,
            Currency = order.Currency,
            SubtotalAmount = order.SubtotalAmount,
            DiscountAmount = order.DiscountAmount,
            TaxAmount = order.TaxAmount,
            TotalAmount = order.TotalAmount,
            PaidAmount = order.PaidAmount,
            PlacedAt = order.PlacedAt,
            PaidAt = order.PaidAt,
            CompletedAt = order.CompletedAt,
            CancelledAt = order.CancelledAt,
            Items = order.OrderItems
                .OrderBy(item => item.CreatedAt)
                .Select(item => new OrderItemResult
                {
                    Id = item.Id,
                    MenuItemId = item.MenuItemId,
                    ProductId = item.ProductId,
                    ProductVariantId = item.ProductVariantId,
                    RecipeId = item.RecipeId,
                    ClientLineId = item.ClientLineId,
                    ProductCodeSnapshot = item.ProductCodeSnapshot,
                    ProductNameSnapshot = item.ProductNameSnapshot,
                    ProductVariantCodeSnapshot = item.ProductVariantCodeSnapshot,
                    ProductVariantNameSnapshot = item.ProductVariantNameSnapshot,
                    Quantity = item.Quantity,
                    UnitPrice = item.UnitPrice,
                    DiscountAmount = item.DiscountAmount,
                    TotalAmount = item.TotalAmount,
                    Status = item.Status
                })
                .ToList()
        };
    }

    private static string GenerateOrderNumber(DateTimeOffset now)
    {
        return $"ORD-{now:yyyyMMddHHmmss}-{Guid.NewGuid():N}"[..36].ToUpperInvariant();
    }

    private static bool CurrencyMatches(string orderCurrency, string productCurrency)
    {
        return string.Equals(orderCurrency, productCurrency, StringComparison.OrdinalIgnoreCase);
    }

    private static bool MatchesScope(Guid? entityScopeId, Guid? kioskScopeId)
    {
        return entityScopeId is null || entityScopeId == kioskScopeId;
    }

    private static bool IsWithinEffectiveWindow(
        DateTimeOffset? effectiveFrom,
        DateTimeOffset? effectiveTo,
        DateTimeOffset now)
    {
        return (effectiveFrom is null || effectiveFrom <= now) &&
               (effectiveTo is null || effectiveTo >= now);
    }

    private static string? ValidateRecipe(Recipe recipe, ProductVariant productVariant, Guid? organizationId, Guid? storeId, Guid kioskId)
    {
        if (recipe.ProductVariantId != productVariant.Id)
        {
            return "Menu item recipe does not belong to the selected product variant.";
        }

        if (recipe.Status is not (RecipeStatus.Published or RecipeStatus.Active))
        {
            return $"Recipe '{recipe.Name}' is not active.";
        }

        if (!MatchesScope(recipe.OrganizationId, organizationId) ||
            !MatchesScope(recipe.StoreId, storeId) ||
            !MatchesScope(recipe.KioskId, kioskId))
        {
            return $"Recipe '{recipe.Name}' is not available for this kiosk.";
        }

        return null;
    }

    private static string? NormalizeOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private static string? BuildOrderNotes(PlaceOrderRequest request)
    {
        var note = NormalizeOptional(request.Notes);
        if (!request.RuntimeSnapshotId.HasValue && !request.RuntimeSnapshotGeneratedAt.HasValue)
        {
            return note;
        }

        var runtimeNote = JsonSerializer.Serialize(new
        {
            request.RuntimeSnapshotId,
            request.RuntimeSnapshotGeneratedAt
        });

        return note is null ? runtimeNote : $"{note}\n{runtimeNote}";
    }

    private static string BuildRecipeSnapshotJson(Recipe recipe)
    {
        return JsonSerializer.Serialize(new
        {
            recipe.Id,
            recipe.Code,
            recipe.Name,
            recipe.ProductVariantId,
            recipe.Version,
            recipe.Status,
            recipe.EstimatedDurationSeconds,
            recipe.InstructionsSchemaVersion,
            recipe.InstructionsJson
        });
    }
}
