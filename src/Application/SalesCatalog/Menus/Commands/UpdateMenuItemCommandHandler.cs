using Application.SalesCatalog.Abstractions;
using Application.SalesCatalog.Menus.Mapping;
using Application.SalesCatalog.Menus.Results;
using Application.SalesCatalog.Menus.Rules;
using Application.SalesCatalog.Menus.Support;
using Application.Shared.Wrappers;

namespace Application.SalesCatalog.Menus.Commands;

public sealed class UpdateMenuItemCommandHandler
{
    private readonly IMenuStore _menus;

    public UpdateMenuItemCommandHandler(IMenuStore menus)
    {
        _menus = menus;
    }

    public async Task<ApiResult<MenuItemResult>> HandleAsync(
        UpdateMenuItemCommand command,
        CancellationToken cancellationToken = default)
    {
        var menuId = command.MenuId;
        var menuItemId = command.MenuItemId;
        var request = command.Request;
        var updatedByAccountId = command.UpdatedByAccountId;

        var menu = await _menus.GetMenuByIdAsync(menuId, cancellationToken: cancellationToken);
        if (menu is null)
        {
            return ApiResult<MenuItemResult>.Fail("Menu not found.", 404);
        }

        var accessError = MenuManagementCommandRules.ValidateExisting<MenuItemResult>(command.Scope, menu);
        if (accessError is not null)
        {
            return accessError;
        }

        var item = await _menus.GetMenuItemByIdAsync(menuId, menuItemId, asNoTracking: false, cancellationToken);
        if (item is null)
        {
            return ApiResult<MenuItemResult>.Fail("Menu item not found.", 404);
        }

        var newProductVariantId = request.ProductVariantId ?? item.ProductVariantId;
        var productVariant = await _menus.GetProductVariantByIdAsync(newProductVariantId, cancellationToken);
        if (productVariant is null)
        {
            return ApiResult<MenuItemResult>.Fail("Product variant does not exist.", 400);
        }
        var newProductId = productVariant.ProductId;
        var newRecipeId = request.RecipeId ?? item.RecipeId;
        var newCode = string.IsNullOrWhiteSpace(request.Code) ? item.Code : MenuNormalizer.NormalizeCode(request.Code);
        var newDisplayName = string.IsNullOrWhiteSpace(request.DisplayName) ? item.DisplayName : request.DisplayName;
        var newPrice = request.Price ?? item.Price;
        var newDiscount = request.DiscountAmount ?? item.DiscountAmount;
        var newCurrency = string.IsNullOrWhiteSpace(request.Currency) ? item.Currency : request.Currency;
        var newPreparationTime = request.PreparationTimeSeconds ?? item.PreparationTimeSeconds;
        var newEffectiveFrom = request.EffectiveFrom ?? item.EffectiveFrom;
        var newEffectiveTo = request.EffectiveTo ?? item.EffectiveTo;

        var validationError = await MenuItemRequestValidator.ValidateMenuItemFieldsAsync(
            _menus,
            menuId,
            command.Scope.OrganizationId,
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
        item.Description = request.Description is null ? item.Description : MenuNormalizer.TrimToNull(request.Description);
        item.Price = newPrice;
        item.DiscountAmount = newDiscount;
        item.Currency = MenuNormalizer.NormalizeCode(newCurrency);
        item.DisplayOrder = request.DisplayOrder ?? item.DisplayOrder;
        item.PreparationTimeSeconds = newPreparationTime;
        item.ImageUrl = request.ImageUrl is null ? item.ImageUrl : MenuNormalizer.TrimToNull(request.ImageUrl);
        item.EffectiveFrom = newEffectiveFrom;
        item.EffectiveTo = newEffectiveTo;
        item.MetadataJson = request.MetadataJson is null ? item.MetadataJson : MenuNormalizer.TrimToNull(request.MetadataJson);
        item.UpdatedAt = DateTimeOffset.UtcNow;
        item.UpdatedByAccountId = updatedByAccountId;

        await _menus.SaveChangesAsync(cancellationToken);
        return ApiResult<MenuItemResult>.Success(MenuResultMapper.ToItemResult(item), "Menu item updated.");
    }
}
