using Domain.Catalog.Enums;
using Domain.SalesCatalog.Entities;
using System;

namespace Application.SalesCatalog.RuntimeMenus.Rules;

internal static class RuntimeMenuSellabilityRules
{
    public static bool IsSellable(MenuItem item, DateTimeOffset now)
    {
        if (!item.IsCurrentlySellable(now))
        {
            return false;
        }

        if (!item.Product.IsAvailable || !item.ProductVariant.IsAvailable)
        {
            return false;
        }

        if (item.Recipe is null)
        {
            return false;
        }

        return item.Recipe.Status is RecipeStatus.Active or RecipeStatus.Published &&
               (item.Recipe.EffectiveFrom is null || item.Recipe.EffectiveFrom <= now) &&
               (item.Recipe.EffectiveTo is null || item.Recipe.EffectiveTo >= now);
    }
}
