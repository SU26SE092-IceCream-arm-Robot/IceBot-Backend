using Domain.Catalog.Enums;
using Domain.SalesCatalog.Entities;

namespace Application.SalesCatalog.RuntimeMenus.Rules;

internal static class RuntimeMenuSellabilityRules
{
    public static bool IsSellable(MenuItem item, DateTimeOffset now, bool hasActiveProductionRoute)
    {
        if (!item.IsCurrentlySellable(now))
        {
            return false;
        }

        if (!item.Product.IsAvailable || !item.ProductVariant.IsAvailable)
        {
            return false;
        }

        // FulfillmentType is backend-only runtime filtering context; kiosk clients receive only the filtered sales menu.
        if (item.ProductVariant.FulfillmentType != FulfillmentType.MachineProduced)
        {
            return true;
        }

        if (item.Recipe is null)
        {
            return false;
        }

        return item.Recipe.Status is RecipeStatus.Active or RecipeStatus.Published &&
               (item.Recipe.EffectiveFrom is null || item.Recipe.EffectiveFrom <= now) &&
               (item.Recipe.EffectiveTo is null || item.Recipe.EffectiveTo >= now) &&
               hasActiveProductionRoute;
    }
}
