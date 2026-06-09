using Application.SalesCatalog.RuntimeMenus.Results;
using Domain.SalesCatalog.Entities;

namespace Application.SalesCatalog.RuntimeMenus.Mapping;

internal static class RuntimeMenuResultMapper
{
    public static RuntimeMenuItemResult ToResult(MenuItem item)
    {
        var imageUrl = string.IsNullOrWhiteSpace(item.ImageUrl)
            ? item.ProductVariant.ImageUrl ?? item.Product.ImageUrl
            : item.ImageUrl;

        var preparationTimeSeconds = item.PreparationTimeSeconds
            ?? item.ProductVariant.PreparationTimeSeconds
            ?? item.Product.PreparationTimeSeconds
            ?? item.Recipe?.EstimatedDurationSeconds;

        return new RuntimeMenuItemResult
        {
            MenuId = item.MenuId,
            MenuItemId = item.Id,
            ProductId = item.ProductId,
            ProductVariantId = item.ProductVariantId,
            RecipeId = item.RecipeId,
            MenuItemCode = item.Code,
            ProductCode = item.Product.Code,
            ProductVariantCode = item.ProductVariant.Code,
            DisplayName = item.DisplayName,
            Description = item.Description ?? item.ProductVariant.Description ?? item.Product.Description,
            SizeCode = item.ProductVariant.SizeCode,
            Price = item.Price,
            DiscountAmount = item.DiscountAmount,
            FinalPrice = item.Price - item.DiscountAmount,
            Currency = item.Currency,
            PreparationTimeSeconds = preparationTimeSeconds,
            ImageUrl = imageUrl,
            RecipeVersion = item.Recipe?.Version
        };
    }
}
