using Application.SalesCatalog.Abstractions;
using Application.SalesCatalog.Menus.Requests;
using Application.SalesCatalog.Menus.Results;
using Application.Shared.Wrappers;
using Domain.Catalog.Entities;
using Domain.SalesCatalog.Entities;
using Domain.SalesCatalog.Enums;
using Domain.Tenants.Enums;

namespace Application.SalesCatalog.Menus.Services;

public sealed class MenuManagementService
{
    private readonly IMenuStore _menus;

    public MenuManagementService(IMenuStore menus)
    {
        _menus = menus;
    }

    public async Task<PagedResult<MenuResult>> ListMenusAsync(
        string? search,
        Guid? organizationId,
        Guid? storeId,
        Guid? kioskId,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        pageNumber = Math.Max(pageNumber, 1);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var totalCount = await _menus.CountMenusAsync(search, organizationId, storeId, kioskId, cancellationToken);
        var menus = await _menus.ListMenusAsync(search, organizationId, storeId, kioskId, pageNumber, pageSize, cancellationToken);

        return PagedResult<MenuResult>.Success(
            menus.Select(ToResult),
            totalCount,
            pageNumber,
            pageSize);
    }

    public async Task<ApiResult<MenuResult>> GetMenuAsync(Guid menuId, CancellationToken cancellationToken = default)
    {
        var menu = await _menus.GetMenuByIdAsync(menuId, cancellationToken: cancellationToken);
        return menu is null
            ? ApiResult<MenuResult>.Fail("Menu not found.", 404)
            : ApiResult<MenuResult>.Success(ToResult(menu));
    }

    public async Task<ApiResult<MenuResult>> CreateMenuAsync(
        CreateMenuRequest request,
        Guid? createdByAccountId = null,
        CancellationToken cancellationToken = default)
    {
        var validationError = await ValidateMenuFieldsAsync(
            request.Code,
            request.Name,
            request.Currency,
            request.ScopeType,
            request.OrganizationId,
            request.StoreId,
            request.KioskId,
            request.EffectiveFrom,
            request.EffectiveTo,
            null,
            cancellationToken);

        if (validationError is not null)
        {
            return ApiResult<MenuResult>.Fail(validationError);
        }

        var menu = new Menu
        {
            OrganizationId = request.OrganizationId,
            StoreId = request.StoreId,
            KioskId = request.KioskId,
            Code = NormalizeCode(request.Code),
            Name = request.Name.Trim(),
            Description = TrimToNull(request.Description),
            Status = request.Status,
            ScopeType = request.ScopeType,
            Currency = NormalizeCode(request.Currency),
            EffectiveFrom = request.EffectiveFrom,
            EffectiveTo = request.EffectiveTo,
            DisplayOrder = request.DisplayOrder,
            MetadataSchemaVersion = Math.Max(request.MetadataSchemaVersion, 1),
            MetadataJson = TrimToNull(request.MetadataJson),
            CreatedAt = DateTimeOffset.UtcNow,
            CreatedByAccountId = createdByAccountId
        };

        await _menus.AddMenuAsync(menu, cancellationToken);
        await _menus.SaveChangesAsync(cancellationToken);

        return ApiResult<MenuResult>.Success(ToResult(menu), "Menu created.", 201);
    }

    public async Task<ApiResult<MenuResult>> UpdateMenuAsync(
        Guid menuId,
        UpdateMenuRequest request,
        Guid? updatedByAccountId = null,
        CancellationToken cancellationToken = default)
    {
        var menu = await _menus.GetMenuByIdAsync(menuId, asNoTracking: false, cancellationToken);
        if (menu is null)
        {
            return ApiResult<MenuResult>.Fail("Menu not found.", 404);
        }

        var newCode = string.IsNullOrWhiteSpace(request.Code) ? menu.Code : NormalizeCode(request.Code);
        var newName = string.IsNullOrWhiteSpace(request.Name) ? menu.Name : request.Name;
        var newCurrency = string.IsNullOrWhiteSpace(request.Currency) ? menu.Currency : request.Currency;
        var newScopeType = request.ScopeType ?? menu.ScopeType;
        var newOrganizationId = request.OrganizationId ?? menu.OrganizationId;
        var newStoreId = request.StoreId ?? menu.StoreId;
        var newKioskId = request.KioskId ?? menu.KioskId;
        var newEffectiveFrom = request.EffectiveFrom ?? menu.EffectiveFrom;
        var newEffectiveTo = request.EffectiveTo ?? menu.EffectiveTo;

        var validationError = await ValidateMenuFieldsAsync(
            newCode,
            newName,
            newCurrency,
            newScopeType,
            newOrganizationId,
            newStoreId,
            newKioskId,
            newEffectiveFrom,
            newEffectiveTo,
            menu.Id,
            cancellationToken);

        if (validationError is not null)
        {
            return ApiResult<MenuResult>.Fail(validationError);
        }

        menu.OrganizationId = newOrganizationId;
        menu.StoreId = newStoreId;
        menu.KioskId = newKioskId;
        menu.Code = newCode;
        menu.Name = newName.Trim();
        menu.Description = request.Description is null ? menu.Description : TrimToNull(request.Description);
        menu.Status = request.Status ?? menu.Status;
        menu.ScopeType = newScopeType;
        menu.Currency = NormalizeCode(newCurrency);
        menu.EffectiveFrom = newEffectiveFrom;
        menu.EffectiveTo = newEffectiveTo;
        menu.DisplayOrder = request.DisplayOrder ?? menu.DisplayOrder;
        menu.MetadataSchemaVersion = request.MetadataSchemaVersion.HasValue
            ? Math.Max(request.MetadataSchemaVersion.Value, 1)
            : menu.MetadataSchemaVersion;
        menu.MetadataJson = request.MetadataJson is null ? menu.MetadataJson : TrimToNull(request.MetadataJson);
        menu.UpdatedAt = DateTimeOffset.UtcNow;
        menu.UpdatedByAccountId = updatedByAccountId;

        await _menus.SaveChangesAsync(cancellationToken);
        return ApiResult<MenuResult>.Success(ToResult(menu), "Menu updated.");
    }

    public async Task<ApiResult<MenuResult>> SetMenuStatusAsync(
        Guid menuId,
        MenuStatus status,
        Guid? updatedByAccountId = null,
        CancellationToken cancellationToken = default)
    {
        var menu = await _menus.GetMenuByIdAsync(menuId, asNoTracking: false, cancellationToken);
        if (menu is null)
        {
            return ApiResult<MenuResult>.Fail("Menu not found.", 404);
        }

        menu.Status = status;
        menu.UpdatedAt = DateTimeOffset.UtcNow;
        menu.UpdatedByAccountId = updatedByAccountId;
        await _menus.SaveChangesAsync(cancellationToken);

        return ApiResult<MenuResult>.Success(ToResult(menu), "Menu status updated.");
    }

    public async Task<ApiResult<bool>> DeleteMenuAsync(
        Guid menuId,
        Guid? deletedByAccountId = null,
        CancellationToken cancellationToken = default)
    {
        var menu = await _menus.GetMenuByIdAsync(menuId, asNoTracking: false, cancellationToken);
        if (menu is null)
        {
            return ApiResult<bool>.Fail("Menu not found.", 404);
        }

        var now = DateTimeOffset.UtcNow;
        menu.DeletedAt = now;
        menu.DeletedByAccountId = deletedByAccountId;

        foreach (var item in menu.MenuItems)
        {
            item.DeletedAt = now;
            item.DeletedByAccountId = deletedByAccountId;
        }

        await _menus.SaveChangesAsync(cancellationToken);
        return ApiResult<bool>.Success(true, "Menu deleted.");
    }

    public async Task<ApiResult<MenuItemResult>> AddMenuItemAsync(
        Guid menuId,
        CreateMenuItemRequest request,
        Guid? createdByAccountId = null,
        CancellationToken cancellationToken = default)
    {
        var menu = await _menus.GetMenuByIdAsync(menuId, asNoTracking: false, cancellationToken);
        if (menu is null)
        {
            return ApiResult<MenuItemResult>.Fail("Menu not found.", 404);
        }

        var validationError = await ValidateMenuItemFieldsAsync(
            menu.Id,
            request.ProductId,
            request.ProductVariantId,
            request.RecipeId,
            request.Code,
            request.DisplayName,
            request.Price,
            request.DiscountAmount,
            request.Currency,
            request.PreparationTimeSeconds,
            request.EffectiveFrom,
            request.EffectiveTo,
            null,
            cancellationToken);

        if (validationError is not null)
        {
            return ApiResult<MenuItemResult>.Fail(validationError);
        }

        var item = new MenuItem
        {
            MenuId = menu.Id,
            ProductId = request.ProductId,
            ProductVariantId = request.ProductVariantId,
            RecipeId = request.RecipeId,
            Code = NormalizeCode(request.Code),
            DisplayName = request.DisplayName.Trim(),
            Description = TrimToNull(request.Description),
            Status = request.Status,
            Price = request.Price,
            DiscountAmount = request.DiscountAmount,
            Currency = NormalizeCode(request.Currency),
            DisplayOrder = request.DisplayOrder,
            PreparationTimeSeconds = request.PreparationTimeSeconds,
            ImageUrl = TrimToNull(request.ImageUrl),
            EffectiveFrom = request.EffectiveFrom,
            EffectiveTo = request.EffectiveTo,
            MetadataSchemaVersion = Math.Max(request.MetadataSchemaVersion, 1),
            MetadataJson = TrimToNull(request.MetadataJson),
            CreatedAt = DateTimeOffset.UtcNow,
            CreatedByAccountId = createdByAccountId
        };

        await _menus.AddMenuItemAsync(item, cancellationToken);
        await _menus.SaveChangesAsync(cancellationToken);

        return ApiResult<MenuItemResult>.Success(ToItemResult(item), "Menu item created.", 201);
    }

    public async Task<ApiResult<MenuItemResult>> UpdateMenuItemAsync(
        Guid menuId,
        Guid menuItemId,
        UpdateMenuItemRequest request,
        Guid? updatedByAccountId = null,
        CancellationToken cancellationToken = default)
    {
        var item = await _menus.GetMenuItemByIdAsync(menuId, menuItemId, asNoTracking: false, cancellationToken);
        if (item is null)
        {
            return ApiResult<MenuItemResult>.Fail("Menu item not found.", 404);
        }

        var newProductId = request.ProductId ?? item.ProductId;
        var newProductVariantId = request.ProductVariantId ?? item.ProductVariantId;
        var newRecipeId = request.RecipeId ?? item.RecipeId;
        var newCode = string.IsNullOrWhiteSpace(request.Code) ? item.Code : NormalizeCode(request.Code);
        var newDisplayName = string.IsNullOrWhiteSpace(request.DisplayName) ? item.DisplayName : request.DisplayName;
        var newPrice = request.Price ?? item.Price;
        var newDiscount = request.DiscountAmount ?? item.DiscountAmount;
        var newCurrency = string.IsNullOrWhiteSpace(request.Currency) ? item.Currency : request.Currency;
        var newPreparationTime = request.PreparationTimeSeconds ?? item.PreparationTimeSeconds;
        var newEffectiveFrom = request.EffectiveFrom ?? item.EffectiveFrom;
        var newEffectiveTo = request.EffectiveTo ?? item.EffectiveTo;

        var validationError = await ValidateMenuItemFieldsAsync(
            menuId,
            newProductId,
            newProductVariantId,
            newRecipeId,
            newCode,
            newDisplayName,
            newPrice,
            newDiscount,
            newCurrency,
            newPreparationTime,
            newEffectiveFrom,
            newEffectiveTo,
            item.Id,
            cancellationToken);

        if (validationError is not null)
        {
            return ApiResult<MenuItemResult>.Fail(validationError);
        }

        item.ProductId = newProductId;
        item.ProductVariantId = newProductVariantId;
        item.RecipeId = newRecipeId;
        item.Code = newCode;
        item.DisplayName = newDisplayName.Trim();
        item.Description = request.Description is null ? item.Description : TrimToNull(request.Description);
        item.Status = request.Status ?? item.Status;
        item.Price = newPrice;
        item.DiscountAmount = newDiscount;
        item.Currency = NormalizeCode(newCurrency);
        item.DisplayOrder = request.DisplayOrder ?? item.DisplayOrder;
        item.PreparationTimeSeconds = newPreparationTime;
        item.ImageUrl = request.ImageUrl is null ? item.ImageUrl : TrimToNull(request.ImageUrl);
        item.EffectiveFrom = newEffectiveFrom;
        item.EffectiveTo = newEffectiveTo;
        item.MetadataSchemaVersion = request.MetadataSchemaVersion.HasValue
            ? Math.Max(request.MetadataSchemaVersion.Value, 1)
            : item.MetadataSchemaVersion;
        item.MetadataJson = request.MetadataJson is null ? item.MetadataJson : TrimToNull(request.MetadataJson);
        item.UpdatedAt = DateTimeOffset.UtcNow;
        item.UpdatedByAccountId = updatedByAccountId;

        await _menus.SaveChangesAsync(cancellationToken);
        return ApiResult<MenuItemResult>.Success(ToItemResult(item), "Menu item updated.");
    }

    public async Task<ApiResult<MenuItemResult>> SetMenuItemStatusAsync(
        Guid menuId,
        Guid menuItemId,
        MenuItemStatus status,
        Guid? updatedByAccountId = null,
        CancellationToken cancellationToken = default)
    {
        var item = await _menus.GetMenuItemByIdAsync(menuId, menuItemId, asNoTracking: false, cancellationToken);
        if (item is null)
        {
            return ApiResult<MenuItemResult>.Fail("Menu item not found.", 404);
        }

        item.Status = status;
        item.UpdatedAt = DateTimeOffset.UtcNow;
        item.UpdatedByAccountId = updatedByAccountId;
        await _menus.SaveChangesAsync(cancellationToken);

        return ApiResult<MenuItemResult>.Success(ToItemResult(item), "Menu item status updated.");
    }

    public async Task<ApiResult<bool>> DeleteMenuItemAsync(
        Guid menuId,
        Guid menuItemId,
        Guid? deletedByAccountId = null,
        CancellationToken cancellationToken = default)
    {
        var item = await _menus.GetMenuItemByIdAsync(menuId, menuItemId, asNoTracking: false, cancellationToken);
        if (item is null)
        {
            return ApiResult<bool>.Fail("Menu item not found.", 404);
        }

        item.DeletedAt = DateTimeOffset.UtcNow;
        item.DeletedByAccountId = deletedByAccountId;
        await _menus.SaveChangesAsync(cancellationToken);

        return ApiResult<bool>.Success(true, "Menu item deleted.");
    }

    private async Task<string?> ValidateMenuFieldsAsync(
        string code,
        string name,
        string currency,
        TenantScopeType scopeType,
        Guid? organizationId,
        Guid? storeId,
        Guid? kioskId,
        DateTimeOffset? effectiveFrom,
        DateTimeOffset? effectiveTo,
        Guid? excludedMenuId,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(code)) return "Menu code is required.";
        if (string.IsNullOrWhiteSpace(name)) return "Menu name is required.";
        if (string.IsNullOrWhiteSpace(currency)) return "Currency is required.";
        if (effectiveFrom is not null && effectiveTo is not null && effectiveFrom > effectiveTo) return "Menu effectiveFrom cannot be after effectiveTo.";

        var scopeError = ValidateTenantScope(scopeType, organizationId, storeId, kioskId);
        if (scopeError is not null) return scopeError;

        if (await _menus.MenuCodeExistsAsync(organizationId, storeId, kioskId, NormalizeCode(code), excludedMenuId, cancellationToken))
        {
            return "Menu code already exists in this scope.";
        }

        return null;
    }

    private async Task<string?> ValidateMenuItemFieldsAsync(
        Guid menuId,
        Guid productId,
        Guid productVariantId,
        Guid? recipeId,
        string code,
        string displayName,
        decimal price,
        decimal discountAmount,
        string currency,
        int? preparationTimeSeconds,
        DateTimeOffset? effectiveFrom,
        DateTimeOffset? effectiveTo,
        Guid? excludedMenuItemId,
        CancellationToken cancellationToken)
    {
        if (productId == Guid.Empty) return "Product is required.";
        if (productVariantId == Guid.Empty) return "Product variant is required.";
        if (string.IsNullOrWhiteSpace(code)) return "Menu item code is required.";
        if (string.IsNullOrWhiteSpace(displayName)) return "Menu item display name is required.";
        if (price < 0) return "Menu item price cannot be negative.";
        if (discountAmount < 0) return "Menu item discount amount cannot be negative.";
        if (discountAmount > price) return "Menu item discount amount cannot be greater than price.";
        if (string.IsNullOrWhiteSpace(currency)) return "Menu item currency is required.";
        if (preparationTimeSeconds < 0) return "Menu item preparation time cannot be negative.";
        if (effectiveFrom is not null && effectiveTo is not null && effectiveFrom > effectiveTo) return "Menu item effectiveFrom cannot be after effectiveTo.";

        var product = await _menus.GetProductByIdAsync(productId, cancellationToken);
        if (product is null) return "Product does not exist.";

        var variant = await _menus.GetProductVariantByIdAsync(productVariantId, cancellationToken);
        if (variant is null) return "Product variant does not exist.";
        if (variant.ProductId != product.Id) return "Product variant does not belong to product.";

        if (recipeId.HasValue)
        {
            var recipe = await _menus.GetRecipeByIdAsync(recipeId.Value, cancellationToken);
            if (recipe is null) return "Recipe does not exist.";
            if (recipe.ProductVariantId != variant.Id) return "Recipe does not belong to product variant.";
        }

        if (await _menus.MenuItemCodeExistsAsync(menuId, NormalizeCode(code), excludedMenuItemId, cancellationToken))
        {
            return "Menu item code already exists in this menu.";
        }

        return null;
    }

    private static string? ValidateTenantScope(TenantScopeType scopeType, Guid? organizationId, Guid? storeId, Guid? kioskId)
    {
        return scopeType switch
        {
            TenantScopeType.Global when organizationId is not null || storeId is not null || kioskId is not null =>
                "Global menu cannot be assigned to organization, store, or kiosk.",
            TenantScopeType.Organization when organizationId is null || storeId is not null || kioskId is not null =>
                "Organization-scoped menu requires organizationId only.",
            TenantScopeType.Store when organizationId is null || storeId is null || kioskId is not null =>
                "Store-scoped menu requires organizationId and storeId only.",
            TenantScopeType.Kiosk when organizationId is null || storeId is null || kioskId is null =>
                "Kiosk-scoped menu requires organizationId, storeId, and kioskId.",
            TenantScopeType.Device => "Device-scoped menu is not supported.",
            _ => null
        };
    }

    private static MenuResult ToResult(Menu menu)
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
            MetadataSchemaVersion = menu.MetadataSchemaVersion,
            MetadataJson = menu.MetadataJson,
            CreatedAt = menu.CreatedAt,
            UpdatedAt = menu.UpdatedAt,
            Items = menu.MenuItems
                .OrderBy(item => item.DisplayOrder)
                .ThenBy(item => item.DisplayName)
                .Select(ToItemResult)
                .ToList()
        };
    }

    private static MenuItemResult ToItemResult(MenuItem item)
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
            ImageUrl = item.ImageUrl,
            EffectiveFrom = item.EffectiveFrom,
            EffectiveTo = item.EffectiveTo,
            MetadataSchemaVersion = item.MetadataSchemaVersion,
            MetadataJson = item.MetadataJson,
            CreatedAt = item.CreatedAt,
            UpdatedAt = item.UpdatedAt
        };
    }

    private static string NormalizeCode(string value) => value.Trim().ToUpperInvariant();

    private static string? TrimToNull(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
