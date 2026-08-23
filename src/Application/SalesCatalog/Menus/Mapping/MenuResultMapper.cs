using Application.SalesCatalog.Menus.Results;
using Domain.SalesCatalog.Entities;

namespace Application.SalesCatalog.Menus.Mapping;

internal static class MenuResultMapper
{
    public static MenuResult ToResult(Menu menu)
    {
        return new MenuResult
        {
            Id = menu.Id,
            OrganizationId = menu.OrganizationId,
            StoreId = menu.StoreId,
            KioskId = menu.KioskId,
            Code = menu.Code,
            Name = menu.Name,
            Description = menu.Description,
            Status = menu.Status,
            ScopeType = menu.ScopeType,
            Currency = menu.Currency,
            EffectiveFrom = menu.EffectiveFrom,
            EffectiveTo = menu.EffectiveTo,
            DisplayOrder = menu.DisplayOrder,
            CreatedAt = menu.CreatedAt,
            UpdatedAt = menu.UpdatedAt,
            Items = menu.MenuItems
                .OrderBy(item => item.DisplayOrder)
                .ThenBy(item => item.DisplayName)
                .Select(ToItemResult)
                .ToList()
        };
    }

    public static MenuItemResult ToItemResult(MenuItem item)
    {
        return new MenuItemResult
        {
            Id = item.Id,
            MenuId = item.MenuId,
            ProductId = item.ProductId,
            ProductVariantId = item.ProductVariantId,
            RecipeId = item.RecipeId,
            Code = item.Code,
            DisplayName = item.DisplayName,
            Description = item.Description,
            Status = item.Status,
            Price = item.Price,
            DiscountAmount = item.DiscountAmount,
            Currency = item.Currency,
            DisplayOrder = item.DisplayOrder,
            PreparationTimeSeconds = item.PreparationTimeSeconds,
            EffectiveFrom = item.EffectiveFrom,
            EffectiveTo = item.EffectiveTo,
            CreatedAt = item.CreatedAt,
            UpdatedAt = item.UpdatedAt,
            ProductOptionIds = item.ProductOptions.Select(option => option.ProductOptionId).ToList()
        };
    }
}
