using Domain.Catalog.Entities;
using Domain.Catalog.Enums;
using Domain.Common.Enums;
using Domain.Devices.Catalog;
using Domain.Inventory.Entities;
using Domain.Inventory.Enums;
using Domain.SalesCatalog.Enums;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Catalog.Bootstrap;

/// <summary>
/// One-shot repair for the isolated ICEBOT-DEMO runtime fixture.
/// It does not rewrite global or non-demo tenant data.
/// </summary>
public sealed class IceBotDemoRuntimeRepair(IceBotDbContext dbContext)
{
    private const string OrganizationCode = IceBotDemoTenantSeedHostedService.OrganizationCode;
    private const string StoreCode = IceBotDemoTenantSeedHostedService.StoreCode;
    private const string KioskCode = IceBotDemoTenantSeedHostedService.KioskCode;
    private const string ProductCode = "KEM-TUOI-VANI";
    private const string VariantCode = "80G";
    private const string RecipeCode = "KEM-TUOI-VANI-80G-V1";
    private const string MixIngredientCode = "VANILLA-SOFT-SERVE-MIX";
    private const string DeviceCode = "ICEBOT-DEMO-SOFT-SERVE-MACHINE";
    private const string ContainerCode = "MIX_HOPPER";
    private const decimal InitialQuantity = 6000m;

    public async Task<bool> RepairAsync(
        CancellationToken cancellationToken = default,
        bool requireExistingFixture = true)
    {
        var now = DateTimeOffset.UtcNow;
        var organization = await dbContext.Organizations
            .WhereNotDeleted()
            .SingleOrDefaultAsync(item => item.Code == OrganizationCode, cancellationToken)
            ?? (requireExistingFixture
                ? throw new InvalidOperationException($"{OrganizationCode} does not exist.")
                : null);
        if (organization is null)
        {
            return false;
        }
        var kiosk = await dbContext.Kiosks
            .WhereNotDeleted()
            .SingleOrDefaultAsync(item => item.OrganizationId == organization.Id && item.Code == KioskCode, cancellationToken)
            ?? throw new InvalidOperationException($"{KioskCode} does not exist.");
        var ingredient = await dbContext.Ingredients
            .WhereNotDeleted()
            .SingleOrDefaultAsync(item => item.Code == MixIngredientCode, cancellationToken)
            ?? throw new InvalidOperationException($"{MixIngredientCode} does not exist.");

        var variants = await dbContext.ProductVariants
            .WhereNotDeleted()
            .Include(item => item.Product)
            .Include(item => item.Recipes)
                .ThenInclude(item => item.RecipeItems)
            .Where(item => item.Code == VariantCode && item.Product.Code == ProductCode &&
                           (item.Product.OrganizationId == null || item.Product.OrganizationId == organization.Id))
            .ToListAsync(cancellationToken);

        foreach (var variant in variants)
        {
            variant.Product.IsAvailable = true;
            variant.IsAvailable = true;
            foreach (var recipe in variant.Recipes.Where(item => item.Code == RecipeCode && item.Version == 1))
            {
                recipe.Status = RecipeStatus.Active;
                recipe.EffectiveFrom = recipe.EffectiveFrom ?? now;
                var existingRecipeItem = recipe.RecipeItems.Count == 1
                    ? recipe.RecipeItems.First()
                    : null;
                var hasExpectedRecipeItem = recipe.RecipeItems.Count == 1 &&
                    existingRecipeItem?.IngredientId == ingredient.Id &&
                    existingRecipeItem.Quantity == 80m &&
                    string.Equals(existingRecipeItem.Unit, "gram", StringComparison.OrdinalIgnoreCase) &&
                    existingRecipeItem.StepOrder == 1;
                if (!hasExpectedRecipeItem)
                {
                    dbContext.RecipeItems.RemoveRange(recipe.RecipeItems);
                    recipe.RecipeItems.Clear();
                    recipe.RecipeItems.Add(new RecipeItem
                    {
                        RecipeId = recipe.Id,
                        IngredientId = ingredient.Id,
                        Ingredient = ingredient,
                        Quantity = 80m,
                        Unit = "gram",
                        StepOrder = 1,
                        Notes = "Operational hopper consumption per 80 g serving.",
                        CreatedAt = now
                    });
                }
            }
        }

        var menu = await dbContext.Menus
            .Include(item => item.MenuItems)
            .SingleOrDefaultAsync(item => item.OrganizationId == organization.Id &&
                                          item.StoreId == kiosk.StoreId &&
                                          item.KioskId == kiosk.Id &&
                                          item.Code == "ICEBOT-DEMO-MENU", cancellationToken);
        if (menu is not null)
        {
            menu.Status = MenuStatus.Active;
            foreach (var item in menu.MenuItems)
            {
                item.Status = MenuItemStatus.Active;
                item.EffectiveFrom ??= now;
            }
        }

        var device = await dbContext.Devices
            .Include(item => item.IngredientDispenserStates)
            .SingleOrDefaultAsync(item => item.KioskId == kiosk.Id && item.Code == DeviceCode, cancellationToken);
        if (device is not null)
        {
            var state = device.IngredientDispenserStates.SingleOrDefault(item => item.ContainerCode == ContainerCode);
            if (state is null)
            {
                state = new IngredientDispenserState
                {
                    Id = Guid.NewGuid(),
                    DeviceId = device.Id,
                    KioskId = kiosk.Id,
                    IngredientId = ingredient.Id,
                    ContainerCode = ContainerCode,
                    CurrentLevelStatus = IngredientLevelStatus.Full,
                    EstimatedQuantity = InitialQuantity,
                    CapacityQuantity = InitialQuantity,
                    Unit = "gram",
                    LastMeasuredAt = now,
                    IsActive = true,
                    OriginNodeId = Guid.Empty,
                    Version = 1,
                    CreatedAt = now
                };
                state.ChangeTrackingMode(InventoryTrackingMode.ManualEstimate);
                dbContext.IngredientDispenserStates.Add(state);
            }
            else
            {
                state.Reactivate(null, now);
                state.IngredientId = ingredient.Id;
                state.ConfigureContainer(InitialQuantity, "gram");
                state.ChangeTrackingMode(InventoryTrackingMode.ManualEstimate);
                if (state.EstimatedQuantity != InitialQuantity)
                {
                    state.AdjustEstimate(InitialQuantity, now, "DEMO_RUNTIME_REPAIR", Guid.Empty, IngredientLevelStatus.Full);
                }
                else
                {
                    state.CurrentLevelStatus = IngredientLevelStatus.Full;
                    state.LastMeasuredAt = now;
                }
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }
}
