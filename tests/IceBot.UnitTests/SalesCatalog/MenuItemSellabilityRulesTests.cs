using Application.SalesCatalog.Rules;
using Domain.Catalog.Entities;
using Domain.Catalog.Enums;
using Domain.SalesCatalog.Entities;
using Domain.SalesCatalog.Enums;
using Domain.Tenants.Entities;

namespace IceBot.UnitTests.SalesCatalog;

public sealed class MenuItemSellabilityRulesTests
{
    [Fact]
    public void Validate_AcceptsValidMachineProducedItemWithActiveRoute()
    {
        var (item, kiosk) = CreateMachineProducedItem();

        var error = MenuItemSellabilityRules.Validate(item, kiosk, DateTimeOffset.UtcNow, true);

        Assert.Null(error);
    }

    [Fact]
    public void Validate_RejectsRecipeThatReferencesInactiveIngredient()
    {
        var (item, kiosk) = CreateMachineProducedItem();
        item.Recipe!.RecipeItems.Single().Ingredient.IsActive = false;

        var error = MenuItemSellabilityRules.Validate(item, kiosk, DateTimeOffset.UtcNow, true);

        Assert.Equal("Recipe 'Vanilla recipe' references an inactive ingredient.", error);
    }

    [Fact]
    public void Validate_RejectsProductOutsideKioskScope()
    {
        var (item, kiosk) = CreateMachineProducedItem();
        item.Product.KioskId = Guid.NewGuid();

        var error = MenuItemSellabilityRules.Validate(item, kiosk, DateTimeOffset.UtcNow, true);

        Assert.Equal("Product 'Vanilla' is not available for this kiosk.", error);
    }

    [Fact]
    public void Validate_RejectsSoftDeletedProduct()
    {
        var (item, kiosk) = CreateMachineProducedItem();
        item.Product.DeletedAt = DateTimeOffset.UtcNow;

        var error = MenuItemSellabilityRules.Validate(item, kiosk, DateTimeOffset.UtcNow, true);

        Assert.Equal("Product 'Vanilla' has been deleted.", error);
    }

    [Fact]
    public void Validate_RejectsAttachedInactiveRecipeForManualItem()
    {
        var (item, kiosk) = CreateMachineProducedItem();
        item.ProductVariant.FulfillmentType = FulfillmentType.Manual;
        item.Recipe!.Status = RecipeStatus.Retired;

        var error = MenuItemSellabilityRules.Validate(item, kiosk, DateTimeOffset.UtcNow, false);

        Assert.Equal("Recipe 'Vanilla recipe' is not active at this time.", error);
    }

    private static (MenuItem Item, Kiosk Kiosk) CreateMachineProducedItem()
    {
        var organizationId = Guid.NewGuid();
        var storeId = Guid.NewGuid();
        var kioskId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var variantId = Guid.NewGuid();
        var recipeId = Guid.NewGuid();

        var kiosk = new Kiosk
        {
            Id = kioskId,
            OrganizationId = organizationId,
            StoreId = storeId,
            Code = "KIOSK-1",
            Name = "Kiosk 1"
        };
        var product = new Product
        {
            Id = productId,
            OrganizationId = organizationId,
            Code = "VANILLA",
            Name = "Vanilla",
            IsAvailable = true
        };
        var variant = new ProductVariant
        {
            Id = variantId,
            ProductId = productId,
            Code = "VANILLA-CUP",
            Name = "Vanilla cup",
            IsAvailable = true,
            FulfillmentType = FulfillmentType.MachineProduced,
            Product = product
        };
        var recipe = new Recipe
        {
            Id = recipeId,
            OrganizationId = organizationId,
            ProductVariantId = variantId,
            Code = "VANILLA-R1",
            Name = "Vanilla recipe",
            Status = RecipeStatus.Active,
            ProductVariant = variant
        };
        recipe.RecipeItems.Add(new RecipeItem
        {
            RecipeId = recipeId,
            IngredientId = Guid.NewGuid(),
            Quantity = 100,
            StepOrder = 1,
            Ingredient = new Ingredient
            {
                Code = "MILK",
                Name = "Milk",
                IsActive = true
            },
            Recipe = recipe
        });
        var menu = new Menu
        {
            OrganizationId = organizationId,
            Code = "DEFAULT",
            Name = "Default",
            Status = MenuStatus.Active
        };
        var item = new MenuItem
        {
            Menu = menu,
            MenuId = Guid.NewGuid(),
            Product = product,
            ProductId = productId,
            ProductVariant = variant,
            ProductVariantId = variantId,
            Recipe = recipe,
            RecipeId = recipeId,
            Code = "VANILLA",
            DisplayName = "Vanilla",
            Status = MenuItemStatus.Active
        };

        return (item, kiosk);
    }
}
