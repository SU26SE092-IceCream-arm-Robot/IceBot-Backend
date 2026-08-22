using Application.Devices.Telemetry;
using Application.Inventory.Abstractions;
using Application.Orders.Abstractions;
using Application.SalesCatalog.Availability;
using Application.SalesCatalog.Rules;
using Application.SalesCatalog.Admission.Services;
using Domain.Catalog.Enums;
using Domain.Orders.Entities;
using Microsoft.Extensions.Options;

namespace Application.Orders.Admission;

/// <summary>Rechecks current order admission immediately before a payment session is opened.</summary>
public sealed class OrderPaymentSellabilityGuard(
    IOrderStore orders,
    IMenuItemOperationalAvailabilityReader operationalAvailability,
    MachineProductionInventoryGate inventoryGate,
    IOptions<EdgeTelemetryIngestionOptions> telemetryOptions,
    MenuItemOperationalAdmissionEvaluator? operationalAdmission = null)
{
    private readonly EdgeTelemetryIngestionOptions _telemetryOptions = telemetryOptions.Value;

    public async Task<string?> ValidateAsync(Order order, DateTimeOffset now, CancellationToken cancellationToken)
    {
        foreach (var orderItem in order.OrderItems.Where(item =>
                     item.FulfillmentType == FulfillmentType.MachineProduced))
        {
            var optionIngredients = orderItem.Options
                .SelectMany(option => option.IngredientRequirements)
                .Select(requirement => new InventoryIngredientRequirementInput(
                    requirement.IngredientId,
                    requirement.IngredientCodeSnapshot,
                    requirement.IngredientNameSnapshot,
                    requirement.QuantityPerOption,
                    requirement.Unit))
                .ToArray();

            if (operationalAdmission is not null)
            {
                var decision = await operationalAdmission.EvaluateAsync(
                    order.Kiosk,
                    orderItem.MenuItemId,
                    orderItem.Quantity,
                    optionIngredients,
                    now,
                    cancellationToken);
                if (!decision.CanSell)
                {
                    return decision.ToDisplayMessage(orderItem.MenuItemNameSnapshot);
                }

                continue;
            }

            var menuItem = await orders.GetMenuItemForKioskAsync(
                orderItem.MenuItemId,
                order.Kiosk.OrganizationId,
                order.Kiosk.StoreId,
                order.KioskId,
                cancellationToken);
            if (menuItem is null || await operationalAvailability.IsPausedAsync(order.KioskId, orderItem.MenuItemId, cancellationToken))
            {
                return $"Menu item '{orderItem.MenuItemNameSnapshot}' is no longer available.";
            }

            var routePolicy = menuItem.Recipe is null
                ? null
                : await orders.GetActiveProductionRouteOptionPolicyAsync(
                    order.KioskId,
                    menuItem.ProductVariantId,
                    menuItem.Recipe.Id,
                    now.AddSeconds(-_telemetryOptions.ReadinessTimeoutSeconds),
                    cancellationToken);
            var sellabilityError = MenuItemSellabilityRules.Validate(menuItem, order.Kiosk, now, routePolicy is not null);
            if (sellabilityError is not null)
            {
                return sellabilityError;
            }

            var inventoryDecision = await inventoryGate.EvaluateAsync(
                order.Kiosk,
                menuItem,
                routePolicy,
                orderItem.Quantity,
                optionIngredients,
                now,
                cancellationToken);
            if (!inventoryDecision.CanSell)
            {
                return inventoryDecision.Reason;
            }
        }

        return null;
    }
}
