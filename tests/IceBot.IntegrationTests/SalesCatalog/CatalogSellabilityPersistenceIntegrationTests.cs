using Domain.Catalog.Entities;
using Domain.Catalog.Enums;
using Domain.SalesCatalog.Entities;
using Domain.SalesCatalog.Enums;
using Domain.Tenants.Entities;
using Domain.Tenants.Enums;
using IceBot.IntegrationTests.Infrastructure;
using Infrastructure.SalesCatalog.Persistence;

namespace IceBot.IntegrationTests.SalesCatalog;

[Collection(IntegrationTestFixture.CollectionName)]
public sealed class CatalogSellabilityPersistenceIntegrationTests(IntegrationTestFixture fixture)
{
    [IntegrationFact]
    public async Task MenuOptionPolicyQuery_IncludesUnassignedRequiredGroup_AndInactiveIngredientState()
    {
        await using var db = fixture.CreateDbContext();
        var now = DateTimeOffset.UtcNow;
        var organization = new Organization
        {
            Id = Guid.CreateVersion7(),
            Code = $"ORG-{Guid.NewGuid():N}",
            Name = "Sellability test organization",
            CreatedAt = now
        };
        var product = new Product
        {
            Id = Guid.CreateVersion7(),
            OrganizationId = organization.Id,
            ScopeType = TenantScopeType.Organization,
            Code = $"PRODUCT-{Guid.NewGuid():N}",
            Name = "Test product",
            Currency = "VND",
            IsAvailable = true,
            CreatedAt = now
        };
        var variant = new ProductVariant
        {
            Id = Guid.CreateVersion7(),
            ProductId = product.Id,
            Code = $"VARIANT-{Guid.NewGuid():N}",
            Name = "Test variant",
            Currency = "VND",
            IsAvailable = true,
            FulfillmentType = FulfillmentType.Manual,
            CreatedAt = now
        };
        var assignedGroup = new OptionGroup
        {
            ProductId = product.Id,
            Code = $"ASSIGNED-{Guid.NewGuid():N}",
            Name = "Assigned group",
            SelectionType = OptionSelectionType.Single,
            MinSelections = 1,
            MaxSelections = 1,
            IsRequired = true,
            IsActive = true,
            CreatedAt = now
        };
        var missingGroup = new OptionGroup
        {
            ProductId = product.Id,
            Code = $"MISSING-{Guid.NewGuid():N}",
            Name = "Missing group",
            SelectionType = OptionSelectionType.Single,
            MinSelections = 1,
            MaxSelections = 1,
            IsRequired = true,
            IsActive = true,
            CreatedAt = now
        };
        var option = new ProductOption
        {
            Id = Guid.CreateVersion7(),
            OptionGroup = assignedGroup,
            Code = $"OPTION-{Guid.NewGuid():N}",
            Name = "Inactive ingredient option",
            ExecutionImpact = ProductOptionExecutionImpact.ProductionAffecting,
            IsAvailable = true,
            CreatedAt = now
        };
        var ingredient = new Ingredient
        {
            Id = Guid.CreateVersion7(),
            Code = $"INGREDIENT-{Guid.NewGuid():N}",
            Name = "Inactive ingredient",
            IsActive = false,
            CreatedAt = now
        };
        var requirement = new ProductOptionIngredientRequirement
        {
            Id = Guid.CreateVersion7(),
            ProductOption = option,
            Ingredient = ingredient,
            Quantity = 1,
            Unit = "gram",
            RequiredWorkcellCapabilityCode = "INGREDIENT_DISPENSER",
            CreatedAt = now
        };
        var menu = new Menu
        {
            Id = Guid.CreateVersion7(),
            OrganizationId = organization.Id,
            ScopeType = TenantScopeType.Organization,
            Code = $"MENU-{Guid.NewGuid():N}",
            Name = "Test menu",
            Status = MenuStatus.Active,
            Currency = "VND",
            CreatedAt = now
        };
        var menuItem = new MenuItem
        {
            Id = Guid.CreateVersion7(),
            MenuId = menu.Id,
            ProductId = product.Id,
            ProductVariantId = variant.Id,
            Code = $"ITEM-{Guid.NewGuid():N}",
            DisplayName = "Test item",
            Status = MenuItemStatus.Active,
            Currency = "VND",
            CreatedAt = now
        };
        var membership = new MenuItemProductOption
        {
            Id = Guid.CreateVersion7(),
            MenuItemId = menuItem.Id,
            ProductOptionId = option.Id,
            CreatedAt = now
        };

        db.AddRange(
            organization,
            product,
            variant,
            assignedGroup,
            missingGroup,
            option,
            ingredient,
            requirement,
            menu,
            menuItem,
            membership);
        await db.SaveChangesAsync();

        var store = new MenuStore(db);
        var groups = await store.ListMenuItemOptionGroupsAsync([menuItem.Id]);
        var options = await store.ListMenuItemProductOptionsAsync([menuItem.Id]);

        Assert.Equal(2, groups.Count);
        Assert.Contains(groups, group => group.OptionGroupCode == missingGroup.Code);
        Assert.Single(options);
        Assert.False(options[0].AreIngredientRequirementsActive);
    }
}
