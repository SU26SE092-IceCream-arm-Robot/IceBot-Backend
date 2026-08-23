using Application.Orders.Abstractions;
using Application.Orders.PlaceOrder.Requests;
using Application.Orders.PlaceOrder.Support;
using Application.SalesCatalog.ReadModels;
using Application.SalesCatalog.Rules;
using Domain.Catalog.Enums;
using Domain.Orders.Entities;
using Domain.Tenants.Entities;
using Application.Inventory.Abstractions;
using Application.SalesCatalog.Admission.Abstractions;

namespace Application.Orders.PlaceOrder.Services;

public sealed record PlaceOrderItemAppendFailure(string Message, int StatusCode);

public sealed class PlaceOrderItemAppender(
    IOrderStore orderStore,
    IMenuItemOperationalAdmissionEvaluator operationalAdmission)
{
    public async Task<PlaceOrderItemAppendFailure?> AppendAsync(
        Order order,
        Kiosk kiosk,
        PlaceOrderItemRequest itemRequest,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var menuItem = await orderStore.GetMenuItemForKioskAsync(
            itemRequest.MenuItemId,
            kiosk.OrganizationId,
            kiosk.StoreId,
            kiosk.Id,
            cancellationToken);
        if (menuItem is null)
            return new($"Menu item '{itemRequest.MenuItemId}' not found.", 404);

        var product = menuItem.Product;
        var productVariant = menuItem.ProductVariant;
        var recipe = menuItem.Recipe;

        if (!order.OrderItems.Any())
            order.SetCurrency(menuItem.Currency);
        else if (!string.Equals(order.Currency, menuItem.Currency, StringComparison.OrdinalIgnoreCase))
            return new("All order items must use the same currency.", 400);

        var selectableOptions = await orderStore.ListMenuItemProductOptionsAsync(menuItem.Id, cancellationToken);
        var optionGroups = await orderStore.ListMenuItemOptionGroupsAsync(menuItem.Id, cancellationToken);
        var selectedOptionIds = itemRequest.SelectedOptions.Select(option => option.ProductOptionId).ToArray();
        var optionError = ProductOptionSelectionRules.Validate(optionGroups, selectableOptions, selectedOptionIds);
        if (optionError is not null)
            return new(optionError, 409);

        var selectedOptions = selectableOptions
            .Where(option => selectedOptionIds.Contains(option.ProductOptionId))
            .OrderBy(option => option.OptionGroupId)
            .ThenBy(option => option.DisplayOrder)
            .ToArray();
        if (productVariant.FulfillmentType == FulfillmentType.Packaged && selectedOptions.Any(option =>
                option.ExecutionImpact == ProductOptionExecutionImpact.ProductionAffecting))
            return new(
                $"Packaged menu item '{menuItem.DisplayName}' cannot use production-affecting options. Use a product variant for physical packaged choices.",
                409);

        var optionIngredientRequirements = await orderStore.ListProductOptionIngredientRequirementsAsync(
            selectedOptions.Select(option => option.ProductOptionId).ToArray(), cancellationToken);
        if (optionIngredientRequirements.Any(requirement => !requirement.IsIngredientActive))
            return new("One or more selected options require an inactive ingredient.", 409);

        var decision = await operationalAdmission.EvaluateAsync(
            kiosk,
            menuItem.Id,
            itemRequest.Quantity,
            optionIngredientRequirements.Select(requirement => new InventoryIngredientRequirementInput(
                requirement.IngredientId,
                requirement.IngredientCode,
                requirement.IngredientName,
                requirement.Quantity,
                requirement.Unit)).ToArray(),
            now,
            cancellationToken);
        if (!decision.CanSell)
        {
            return new(decision.ToDisplayMessage(menuItem.DisplayName)!, 409);
        }

        if (productVariant.FulfillmentType == FulfillmentType.MachineProduced &&
            selectedOptions.Any(option =>
                option.ExecutionImpact == ProductOptionExecutionImpact.ProductionAffecting &&
                !decision.SupportedProductionOptionCodes.Contains(option.Code)))
        {
            return new(
                $"One or more selected options are not supported by the active production route for '{menuItem.DisplayName}'.",
                409);
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
            productVariant.FulfillmentType,
            itemRequest.Quantity,
            menuItem.Price + selectedOptions.Sum(option => option.PriceDelta),
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

        return null;
    }

    private static string? NormalizeOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
