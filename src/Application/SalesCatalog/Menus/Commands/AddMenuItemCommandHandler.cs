using Application.SalesCatalog.Abstractions;
using Application.SalesCatalog.Menus.Mapping;
using Application.SalesCatalog.Menus.Results;
using Application.SalesCatalog.Menus.Rules;
using Application.SalesCatalog.Menus.Support;
using Application.Shared.Wrappers;
using Domain.SalesCatalog.Entities;

namespace Application.SalesCatalog.Menus.Commands;

public sealed class AddMenuItemCommandHandler
{
    private readonly IMenuStore _menus;

    public AddMenuItemCommandHandler(IMenuStore menus)
    {
        _menus = menus;
    }

    public async Task<ApiResult<MenuItemResult>> HandleAsync(
        AddMenuItemCommand command,
        CancellationToken cancellationToken = default)
    {
        var menuId = command.MenuId;
        var request = command.Request;
        var createdByAccountId = command.CreatedByAccountId;

        var menu = await _menus.GetMenuByIdAsync(menuId, asNoTracking: false, cancellationToken);
        if (menu is null)
        {
            return ApiResult<MenuItemResult>.Fail("Menu not found.", 404);
        }

        var validationError = await MenuItemRequestValidator.ValidateMenuItemFieldsAsync(
            _menus,
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
            Code = MenuNormalizer.NormalizeCode(request.Code),
            DisplayName = request.DisplayName.Trim(),
            Description = MenuNormalizer.TrimToNull(request.Description),
            Status = request.Status,
            Price = request.Price,
            DiscountAmount = request.DiscountAmount,
            Currency = MenuNormalizer.NormalizeCode(request.Currency),
            DisplayOrder = request.DisplayOrder,
            PreparationTimeSeconds = request.PreparationTimeSeconds,
            ImageUrl = MenuNormalizer.TrimToNull(request.ImageUrl),
            EffectiveFrom = request.EffectiveFrom,
            EffectiveTo = request.EffectiveTo,
            MetadataSchemaVersion = Math.Max(request.MetadataSchemaVersion, 1),
            MetadataJson = MenuNormalizer.TrimToNull(request.MetadataJson),
            CreatedAt = DateTimeOffset.UtcNow,
            CreatedByAccountId = createdByAccountId
        };

        await _menus.AddMenuItemAsync(item, cancellationToken);
        await _menus.SaveChangesAsync(cancellationToken);

        return ApiResult<MenuItemResult>.Success(MenuResultMapper.ToItemResult(item), "Menu item created.", 201);
    }
}
