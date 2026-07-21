using Application.SalesCatalog.Abstractions;
using Application.SalesCatalog.Rules;
using Domain.Catalog.Enums;
using Domain.SalesCatalog.Entities;

namespace Application.SalesCatalog.Menus.Rules;

internal static class MenuItemActivationRules
{
    public static async Task<string?> ValidateAsync(
        IMenuStore menus,
        Menu menu,
        MenuItem item,
        CancellationToken cancellationToken)
    {
        var product = await menus.GetProductByIdAsync(item.ProductId, cancellationToken);
        if (product is null) return "Menu item product does not exist or has been deleted.";
        if (!string.Equals(product.Currency, menu.Currency, StringComparison.OrdinalIgnoreCase))
            return "Menu item product currency must match the menu currency before activation.";

        var variant = await menus.GetProductVariantByIdAsync(item.ProductVariantId, cancellationToken);
        if (variant is null || variant.ProductId != product.Id)
            return "Menu item product variant does not belong to its product.";

        if (variant.FulfillmentType == FulfillmentType.MachineProduced && !item.RecipeId.HasValue)
            return "Machine-produced menu items require a recipe before activation.";

        if (item.RecipeId.HasValue)
        {
            var recipe = await menus.GetRecipeByIdAsync(item.RecipeId.Value, cancellationToken);
            if (recipe is null || recipe.ProductVariantId != variant.Id)
                return "Menu item recipe does not belong to its product variant.";
            if (recipe.Status is not (RecipeStatus.Published or RecipeStatus.Active))
                return "Menu item recipe must be Published or Active before activation.";
            if (recipe.RecipeItems.Any(recipeItem => !recipeItem.Ingredient.IsActive))
                return "Menu item recipe references an inactive ingredient.";
        }

        var groups = await menus.ListMenuItemOptionGroupsAsync([item.Id], cancellationToken);
        var options = await menus.ListMenuItemProductOptionsAsync([item.Id], cancellationToken);
        var staticallyEligibleOptions = variant.FulfillmentType == FulfillmentType.Packaged
            ? options.Where(option => option.ExecutionImpact == ProductOptionExecutionImpact.CommercialOnly).ToArray()
            : options.ToArray();
        return ProductOptionSelectionRules.IsSatisfiable(groups, staticallyEligibleOptions)
            ? null
            : "Menu item option configuration cannot satisfy one or more required option groups.";
    }
}
