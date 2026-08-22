using Domain.Catalog.Enums;
using Domain.SalesCatalog.Entities;
using Domain.SalesCatalog.Enums;
using Domain.Tenants.Entities;

namespace Application.SalesCatalog.Rules;

public static class MenuItemSellabilityRules
{
    public static string? Validate(
        MenuItem item,
        Kiosk kiosk,
        DateTimeOffset now,
        bool hasActiveProductionRoute)
    {
        var staticValidationError = ValidateStatic(item, kiosk, now);
        if (staticValidationError is not null)
        {
            return staticValidationError;
        }

        if (item.ProductVariant.FulfillmentType == FulfillmentType.MachineProduced && !hasActiveProductionRoute)
        {
            return $"Menu item '{item.DisplayName}' does not have an active production route for this kiosk.";
        }

        return null;
    }

    /// <summary>
    /// Validates catalog facts that remain valid for the lifetime of a runtime-menu projection.
    /// Route readiness, inventory, and kiosk operational evidence are deliberately excluded.
    /// </summary>
    public static string? ValidateStatic(
        MenuItem item,
        Kiosk kiosk,
        DateTimeOffset now)
    {
        if (item.Menu.Status != MenuStatus.Active ||
            !IsWithinEffectiveWindow(item.Menu.EffectiveFrom, item.Menu.EffectiveTo, now))
        {
            return $"Menu '{item.Menu.Name}' is not active at this time.";
        }

        if (item.Menu.OrganizationId != kiosk.OrganizationId ||
            !MatchesOptionalScope(item.Menu.StoreId, kiosk.StoreId) ||
            !MatchesOptionalScope(item.Menu.KioskId, kiosk.Id))
        {
            return $"Menu '{item.Menu.Name}' is not available for this kiosk.";
        }

        if (!item.IsCurrentlySellable(now))
        {
            return $"Menu item '{item.DisplayName}' is not available.";
        }

        if (item.Product.DeletedAt.HasValue)
        {
            return $"Product '{item.Product.Name}' has been deleted.";
        }

        if (!item.Product.IsAvailable)
        {
            return $"Product '{item.Product.Name}' is not available.";
        }

        if (!item.ProductVariant.IsAvailable)
        {
            return $"Product variant '{item.ProductVariant.Name}' is not available.";
        }

        if (item.ProductVariant.ProductId != item.Product.Id)
        {
            return "Menu item variant does not belong to the selected product.";
        }

        if (item.Product.OrganizationId != kiosk.OrganizationId ||
            !MatchesOptionalScope(item.Product.StoreId, kiosk.StoreId) ||
            !MatchesOptionalScope(item.Product.KioskId, kiosk.Id))
        {
            return $"Product '{item.Product.Name}' is not available for this kiosk.";
        }

        var recipe = item.Recipe;
        if (item.ProductVariant.FulfillmentType == FulfillmentType.MachineProduced && recipe is null)
        {
            return $"Menu item '{item.DisplayName}' requires a recipe.";
        }

        if (recipe is not null)
        {
            if (recipe.ProductVariantId != item.ProductVariant.Id)
            {
                return "Menu item recipe does not belong to the selected product variant.";
            }

            if (recipe.Status is not (RecipeStatus.Published or RecipeStatus.Active) ||
                !IsWithinEffectiveWindow(recipe.EffectiveFrom, recipe.EffectiveTo, now))
            {
                return $"Recipe '{recipe.Name}' is not active at this time.";
            }

            if (recipe.OrganizationId != kiosk.OrganizationId ||
                !MatchesOptionalScope(recipe.StoreId, kiosk.StoreId) ||
                !MatchesOptionalScope(recipe.KioskId, kiosk.Id))
            {
                return $"Recipe '{recipe.Name}' is not available for this kiosk.";
            }

            if (recipe.RecipeItems.Any(recipeItem => !recipeItem.Ingredient.IsActive))
            {
                return $"Recipe '{recipe.Name}' references an inactive ingredient.";
            }
        }

        return null;
    }

    private static bool MatchesOptionalScope(Guid? entityScopeId, Guid kioskScopeId) =>
        !entityScopeId.HasValue || entityScopeId == kioskScopeId;

    private static bool IsWithinEffectiveWindow(
        DateTimeOffset? effectiveFrom,
        DateTimeOffset? effectiveTo,
        DateTimeOffset now) =>
        (effectiveFrom is null || effectiveFrom <= now) &&
        (effectiveTo is null || effectiveTo >= now);
}
