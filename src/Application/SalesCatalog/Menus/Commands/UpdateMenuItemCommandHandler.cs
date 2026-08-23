using Application.SalesCatalog.Abstractions;
using Application.SalesCatalog.Menus.Mapping;
using Application.SalesCatalog.Menus.Results;
using Application.SalesCatalog.Menus.Rules;
using Application.SalesCatalog.Menus.Support;
using Application.Shared.Wrappers;
using Application.Shared.Concurrency;
using Domain.Catalog.Entities;

namespace Application.SalesCatalog.Menus.Commands;

public sealed class UpdateMenuItemCommandHandler
{
    private readonly IMenuStore _menus;
    private readonly ITechnicalResourceMutationCoordinator _mutations;

    public UpdateMenuItemCommandHandler(
        IMenuStore menus,
        ITechnicalResourceMutationCoordinator mutations)
    {
        _menus = menus;
        _mutations = mutations;
    }

    public async Task<ApiResult<MenuItemResult>> HandleAsync(
        UpdateMenuItemCommand command,
        CancellationToken cancellationToken = default)
    {
        var menuId = command.MenuId;
        var menuItemId = command.MenuItemId;
        var request = command.Request;

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

        var productLocks = new[] { item.ProductId, productVariant.ProductId }
            .Distinct()
            .Select(TechnicalResourceMutationIdentity.Product)
            .ToArray();
        return await _mutations.ExecuteAsync(
            productLocks,
            ct => UpdateLockedAsync(command, menu, item, productVariant, ct),
            cancellationToken);
    }

    private async Task<ApiResult<MenuItemResult>> UpdateLockedAsync(
        UpdateMenuItemCommand command,
        Domain.SalesCatalog.Entities.Menu menu,
        Domain.SalesCatalog.Entities.MenuItem item,
        ProductVariant productVariant,
        CancellationToken cancellationToken)
    {
        var request = command.Request;
        var updatedByAccountId = command.UpdatedByAccountId;
        var menuId = command.MenuId;
        var newProductId = productVariant.ProductId;
        if (request.ClearRecipe && request.RecipeId.HasValue)
            return ApiResult<MenuItemResult>.Fail("RecipeId and ClearRecipe cannot be supplied together.");
        var newRecipeId = request.ClearRecipe ? null : request.RecipeId ?? item.RecipeId;
        var newCode = string.IsNullOrWhiteSpace(request.Code) ? item.Code : MenuNormalizer.NormalizeCode(request.Code);
        var newDisplayName = string.IsNullOrWhiteSpace(request.DisplayName) ? item.DisplayName : request.DisplayName;
        var newPrice = request.Price ?? item.Price;
        var newDiscount = request.DiscountAmount ?? item.DiscountAmount;
        var newCurrency = menu.Currency;
        var newPreparationTime = request.PreparationTimeSeconds ?? item.PreparationTimeSeconds;
        var newEffectiveFrom = request.EffectiveFrom ?? item.EffectiveFrom;
        var newEffectiveTo = request.EffectiveTo ?? item.EffectiveTo;

        var validationError = await MenuItemRequestValidator.ValidateMenuItemFieldsAsync(
            _menus,
            menuId,
            command.Scope.OrganizationId,
            newProductId,
            productVariant.Id,
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

        ProductOption[]? replacementOptions = null;
        if (request.ProductOptionIds is not null)
        {
            var optionIds = request.ProductOptionIds.Distinct().ToArray();
            if (optionIds.Length != request.ProductOptionIds.Count)
            {
                return ApiResult<MenuItemResult>.Fail("Product option ids must be unique.");
            }

            replacementOptions = (await _menus.ListProductOptionsAsync(newProductId, optionIds, cancellationToken)).ToArray();
            if (replacementOptions.Length != optionIds.Length)
            {
                return ApiResult<MenuItemResult>.Fail("Every product option must belong to the selected product.", 409);
            }

        }

        item.ProductId = newProductId;
        item.ProductVariantId = productVariant.Id;
        item.RecipeId = newRecipeId;
        item.Code = newCode;
        item.DisplayName = newDisplayName.Trim();
        item.Description = request.Description is null ? item.Description : MenuNormalizer.TrimToNull(request.Description);
        item.Price = newPrice;
        item.DiscountAmount = newDiscount;
        item.Currency = MenuNormalizer.NormalizeCode(newCurrency);
        item.DisplayOrder = request.DisplayOrder ?? item.DisplayOrder;
        item.PreparationTimeSeconds = newPreparationTime;
        item.EffectiveFrom = newEffectiveFrom;
        item.EffectiveTo = newEffectiveTo;
        item.UpdatedAt = DateTimeOffset.UtcNow;
        item.UpdatedByAccountId = updatedByAccountId;

        if (request.ProductOptionIds is not null)
        {
            var replacements = replacementOptions!
                .Select(option => new Domain.SalesCatalog.Entities.MenuItemProductOption
                {
                    ProductOptionId = option.Id,
                    CreatedAt = item.UpdatedAt.Value,
                    CreatedByAccountId = updatedByAccountId
                })
                .ToArray();
            _menus.ReplaceMenuItemProductOptions(item, replacements);
        }

        await _menus.SaveChangesAsync(cancellationToken);
        return ApiResult<MenuItemResult>.Success(MenuResultMapper.ToItemResult(item), "Menu item updated.");
    }
}
