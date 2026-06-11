using Domain.Catalog.Entities;
using Domain.Catalog.Enums;

namespace Application.Orders.PlaceOrder.Rules;

internal static class RecipeValidationRules
{
    public static string? ValidateRecipe(
        Recipe recipe,
        ProductVariant productVariant,
        Guid? organizationId,
        Guid? storeId,
        Guid kioskId)
    {
        if (recipe.ProductVariantId != productVariant.Id)
        {
            return "Menu item recipe does not belong to the selected product variant.";
        }

        if (recipe.Status is not (RecipeStatus.Published or RecipeStatus.Active))
        {
            return $"Recipe '{recipe.Name}' is not active.";
        }

        if (!PlaceOrderScopeRules.MatchesScope(recipe.OrganizationId, organizationId) ||
            !PlaceOrderScopeRules.MatchesScope(recipe.StoreId, storeId) ||
            !PlaceOrderScopeRules.MatchesScope(recipe.KioskId, kioskId))
        {
            return $"Recipe '{recipe.Name}' is not available for this kiosk.";
        }

        return null;
    }
}
