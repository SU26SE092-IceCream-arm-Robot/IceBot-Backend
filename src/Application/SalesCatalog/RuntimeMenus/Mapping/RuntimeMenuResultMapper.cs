using Application.SalesCatalog.RuntimeMenus.Results;
using Domain.SalesCatalog.Entities;
using Application.SalesCatalog.ReadModels;
using Application.SalesCatalog.Rules;

namespace Application.SalesCatalog.RuntimeMenus.Mapping;

internal static class RuntimeMenuResultMapper
{
    public static RuntimeMenuItemResult ToResult(
        MenuItem item,
        IReadOnlyCollection<MenuItemProductOptionReadModel> availableOptions)
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
            RecipeVersion = item.Recipe?.Version,
            OptionGroups = availableOptions
                .Where(ProductOptionSelectionRules.IsSelectable)
                .GroupBy(option => new
                {
                    option.OptionGroupId,
                    option.OptionGroupCode,
                    option.OptionGroupName,
                    option.SelectionType,
                    option.MinSelections,
                    option.MaxSelections,
                    option.IsRequired
                })
                .OrderBy(group => group.Key.OptionGroupName)
                .Select(group => new RuntimeMenuOptionGroupResult
                {
                    OptionGroupId = group.Key.OptionGroupId,
                    Code = group.Key.OptionGroupCode,
                    Name = group.Key.OptionGroupName,
                    SelectionType = group.Key.SelectionType.ToString(),
                    MinSelections = group.Key.MinSelections,
                    MaxSelections = group.Key.MaxSelections,
                    IsRequired = group.Key.IsRequired,
                    Options = group.OrderBy(option => option.DisplayOrder).ThenBy(option => option.Name)
                        .Select(option => new RuntimeMenuProductOptionResult
                        {
                            ProductOptionId = option.ProductOptionId,
                            Code = option.Code,
                            Name = option.Name,
                            Description = option.Description,
                            PriceDelta = option.PriceDelta,
                            Currency = item.Currency,
                            IsDefault = option.IsDefault
                        }).ToList()
                }).ToList()
        };
    }
}
