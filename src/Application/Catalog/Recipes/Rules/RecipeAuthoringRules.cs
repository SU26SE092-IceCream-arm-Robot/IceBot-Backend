using Application.Catalog.Abstractions;
using Application.Catalog.Products.Commands;
using Application.Catalog.Products.Support;
using Application.Catalog.Recipes.Requests;
using Application.Shared.Wrappers;
using Domain.Catalog.Entities;

namespace Application.Catalog.Recipes.Rules;

internal static class RecipeAuthoringRules
{
    public static async Task<(Product? Product, ProductVariant? Variant, ApiResult<T>? Error)> ResolveAsync<T>(
        ICatalogAuthoringStore catalog,
        ProductManagementCommandScope scope,
        Guid productId,
        Guid variantId,
        CancellationToken ct)
    {
        var product = await catalog.GetProductForRecipeAuthoringAsync(productId, ct);
        if (product is null)
        {
            return (null, null, ApiResult<T>.Fail("Product not found.", 404));
        }

        var access = ProductManagementCommandRules.ValidateExisting<T>(scope, product);
        if (access is not null)
        {
            return (null, null, access);
        }

        var variant = await catalog.GetVariantForRecipeAuthoringAsync(productId, variantId, ct);
        return variant is null
            ? (null, null, ApiResult<T>.Fail("Product variant not found.", 404))
            : (product, variant, null);
    }

    public static string? ValidateRecipe(
        string name,
        decimal yieldQuantity,
        string unit,
        DateTimeOffset? effectiveFrom,
        DateTimeOffset? effectiveTo)
    {
        if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(unit))
        {
            return "Recipe name and unit are required.";
        }

        if (yieldQuantity <= 0)
        {
            return "Recipe yield quantity must be greater than zero.";
        }

        return effectiveFrom.HasValue && effectiveTo.HasValue && effectiveFrom > effectiveTo
            ? "Recipe effectiveFrom must be before effectiveTo."
            : null;
    }

    public static string? ValidateItems(IReadOnlyCollection<RecipeItemRequest> items)
    {
        if (items.Count is < 1 or > 100)
        {
            return "Recipe requires between 1 and 100 ingredient items.";
        }

        if (items.Any(item => item.IngredientId == Guid.Empty || item.Quantity <= 0 ||
                              item.DisplayOrder <= 0 || string.IsNullOrWhiteSpace(item.Unit)))
        {
            return "Recipe item identity, quantity, unit, and display order are required.";
        }

        if (items.Select(item => item.DisplayOrder).Distinct().Count() != items.Count)
        {
            return "Recipe item display orders must be unique.";
        }

        return items.Select(item => item.IngredientId).Distinct().Count() != items.Count
            ? "An ingredient can appear only once in a recipe."
            : null;
    }

    public static string NormalizeCode(string code) => ProductNormalizer.NormalizeCode(code);
    public static string? TrimToNull(string? value) => ProductNormalizer.TrimToNull(value);
}
