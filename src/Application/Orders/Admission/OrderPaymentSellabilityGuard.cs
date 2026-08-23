using Application.Inventory.Abstractions;
using Application.SalesCatalog.Admission.Abstractions;
using Application.SalesCatalog.Admission;
using Domain.Catalog.Enums;
using Domain.Orders.Entities;

namespace Application.Orders.Admission;

public sealed record OrderPaymentSellabilityFailure(SalesAdmissionBlocker Blocker, string Message);

/// <summary>Rechecks current order admission immediately before a payment session is opened.</summary>
public sealed class OrderPaymentSellabilityGuard(
    IMenuItemOperationalAdmissionEvaluator operationalAdmission)
{
    public async Task<OrderPaymentSellabilityFailure?> ValidateAsync(
        Order order,
        DateTimeOffset now,
        CancellationToken cancellationToken)
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

            var decision = await operationalAdmission.EvaluateAsync(
                order.Kiosk,
                orderItem.MenuItemId,
                orderItem.Quantity,
                optionIngredients,
                now,
                cancellationToken);
            if (!decision.CanSell)
            {
                var blocker = decision.PrimaryBlocker
                    ?? throw new InvalidOperationException("Blocked menu item admission must provide a blocker.");
                return new OrderPaymentSellabilityFailure(
                    blocker,
                    decision.ToDisplayMessage(orderItem.MenuItemNameSnapshot)!);
            }
        }

        return null;
    }
}
