using Application.SalesCatalog.Abstractions;
using Application.SalesCatalog.RuntimeMenus.Results;
using Application.Shared.Wrappers;
using Domain.Catalog.Enums;
using Domain.SalesCatalog.Entities;

namespace Application.SalesCatalog.RuntimeMenus.Services;

public sealed class RuntimeMenuService
{
    private static readonly TimeSpan SnapshotTtl = TimeSpan.FromSeconds(15);

    private readonly IMenuStore _menus;

    public RuntimeMenuService(IMenuStore menus)
    {
        _menus = menus;
    }

    public async Task<ApiResult<RuntimeMenuResult>> GetKioskRuntimeMenuAsync(
        Guid kioskId,
        CancellationToken cancellationToken = default)
    {
        var kiosk = await _menus.GetKioskByIdAsync(kioskId, cancellationToken);
        if (kiosk is null)
        {
            return ApiResult<RuntimeMenuResult>.Fail("Kiosk not found.", 404);
        }

        var now = DateTimeOffset.UtcNow;
        var menus = await _menus.ListActiveMenusForKioskAsync(
            kiosk.OrganizationId,
            kiosk.StoreId,
            kiosk.Id,
            now,
            cancellationToken);

        var items = menus
            .SelectMany(menu => menu.MenuItems.Select(item => new { Menu = menu, Item = item }))
            .Where(entry => IsSellable(entry.Item, now))
            .OrderBy(entry => entry.Menu.DisplayOrder)
            .ThenBy(entry => entry.Item.DisplayOrder)
            .ThenBy(entry => entry.Item.DisplayName)
            .Select(entry => ToResult(entry.Item))
            .ToList();

        var result = new RuntimeMenuResult
        {
            SnapshotId = Guid.CreateVersion7(),
            KioskId = kiosk.Id,
            GeneratedAt = now,
            ExpiresAt = now.Add(SnapshotTtl),
            ContainsMachineRuntimeState = false,
            Items = items
        };

        return ApiResult<RuntimeMenuResult>.Success(result);
    }

    private static bool IsSellable(MenuItem item, DateTimeOffset now)
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

    private static RuntimeMenuItemResult ToResult(MenuItem item)
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
