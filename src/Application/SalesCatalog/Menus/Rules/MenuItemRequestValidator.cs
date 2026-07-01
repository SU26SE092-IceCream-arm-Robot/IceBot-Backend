using Application.SalesCatalog.Abstractions;
using Application.SalesCatalog.Menus.Support;

namespace Application.SalesCatalog.Menus.Rules;

internal static class MenuItemRequestValidator
{
    public static async Task<string?> ValidateMenuItemFieldsAsync(
        IMenuStore menus,
        Guid menuId,
        Guid organizationId,
        Guid productId,
        Guid productVariantId,
        Guid? recipeId,
        string code,
        string displayName,
        decimal price,
        decimal discountAmount,
        string currency,
        int? preparationTimeSeconds,
        DateTimeOffset? effectiveFrom,
        DateTimeOffset? effectiveTo,
        Guid? excludedMenuItemId,
        CancellationToken cancellationToken)
    {
        if (productId == Guid.Empty) return "Product is required.";
        if (productVariantId == Guid.Empty) return "Product variant is required.";
        if (string.IsNullOrWhiteSpace(code)) return "Menu item code is required.";
        if (string.IsNullOrWhiteSpace(displayName)) return "Menu item display name is required.";
        if (price < 0) return "Menu item price cannot be negative.";
        if (discountAmount < 0) return "Menu item discount amount cannot be negative.";
        if (discountAmount > price) return "Menu item discount amount cannot be greater than price.";
        if (string.IsNullOrWhiteSpace(currency)) return "Menu item currency is required.";
        if (preparationTimeSeconds < 0) return "Menu item preparation time cannot be negative.";
        if (effectiveFrom is not null && effectiveTo is not null && effectiveFrom > effectiveTo) return "Menu item effectiveFrom cannot be after effectiveTo.";

        var product = await menus.GetProductByIdAsync(productId, cancellationToken);
        if (product is null) return "Product does not exist.";
        if (product.OrganizationId != organizationId) return "Product does not belong to the menu organization.";

        var variant = await menus.GetProductVariantByIdAsync(productVariantId, cancellationToken);
        if (variant is null) return "Product variant does not exist.";
        if (variant.ProductId != product.Id) return "Product variant does not belong to product.";

        if (recipeId.HasValue)
        {
            var recipe = await menus.GetRecipeByIdAsync(recipeId.Value, cancellationToken);
            if (recipe is null) return "Recipe does not exist.";
            if (recipe.ProductVariantId != variant.Id) return "Recipe does not belong to product variant.";
            if (recipe.OrganizationId.HasValue && recipe.OrganizationId != organizationId)
                return "Recipe does not belong to the menu organization.";
        }

        if (await menus.MenuItemCodeExistsAsync(menuId, MenuNormalizer.NormalizeCode(code), excludedMenuItemId, cancellationToken))
        {
            return "Menu item code already exists in this menu.";
        }

        return null;
    }
}
