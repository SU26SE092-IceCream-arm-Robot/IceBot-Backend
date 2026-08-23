using Application.SalesCatalog.Abstractions;
using Application.SalesCatalog.Menus.Mapping;
using Application.SalesCatalog.Menus.Results;
using Application.SalesCatalog.Menus.Rules;
using Application.SalesCatalog.Menus.Support;
using Application.Shared.Wrappers;
using Application.Shared.Concurrency;
using Domain.SalesCatalog.Entities;

namespace Application.SalesCatalog.Menus.Commands;

public sealed class AddMenuItemCommandHandler
{
    private readonly IMenuStore _menus;
    private readonly ITechnicalResourceMutationCoordinator _mutations;

    public AddMenuItemCommandHandler(
        IMenuStore menus,
        ITechnicalResourceMutationCoordinator mutations)
    {
        _menus = menus;
        _mutations = mutations;
    }

    public async Task<ApiResult<MenuItemResult>> HandleAsync(
        AddMenuItemCommand command,
        CancellationToken cancellationToken = default)
    {
        var menuId = command.MenuId;
        var request = command.Request;

        var menu = await _menus.GetMenuByIdAsync(menuId, asNoTracking: true, cancellationToken);
        if (menu is null)
        {
            return ApiResult<MenuItemResult>.Fail("Menu not found.", 404);
        }

        var accessError = MenuManagementCommandRules.ValidateExisting<MenuItemResult>(command.Scope, menu);
        if (accessError is not null)
        {
            return accessError;
        }

        var productVariant = await _menus.GetProductVariantByIdAsync(request.ProductVariantId, cancellationToken);
        if (productVariant is null)
        {
            return ApiResult<MenuItemResult>.Fail("Product variant does not exist.", 400);
        }

        return await _mutations.ExecuteAsync(
            [
                TechnicalResourceMutationIdentity.Menu(menu.Id),
                TechnicalResourceMutationIdentity.Product(productVariant.ProductId)
            ],
            ct => AddLockedAsync(command, productVariant, ct),
            cancellationToken);
    }

    private async Task<ApiResult<MenuItemResult>> AddLockedAsync(
        AddMenuItemCommand command,
        Domain.Catalog.Entities.ProductVariant productVariant,
        CancellationToken cancellationToken)
    {
        var request = command.Request;
        var createdByAccountId = command.CreatedByAccountId;
        var menu = await _menus.GetMenuByIdAsync(command.MenuId, asNoTracking: false, cancellationToken);
        if (menu is null)
            return ApiResult<MenuItemResult>.Fail("Menu not found.", 404);
        var accessError = MenuManagementCommandRules.ValidateExisting<MenuItemResult>(command.Scope, menu);
        if (accessError is not null) return accessError;

        var validationError = await MenuItemRequestValidator.ValidateMenuItemFieldsAsync(
            _menus,
            menu.Id,
            command.Scope.OrganizationId,
            productVariant.ProductId,
            request.ProductVariantId,
            request.RecipeId,
            request.Code,
            request.DisplayName,
            request.Price,
            request.DiscountAmount,
            menu.Currency,
            request.PreparationTimeSeconds,
            request.EffectiveFrom,
            request.EffectiveTo,
            null,
            cancellationToken);

        if (validationError is not null)
        {
            return ApiResult<MenuItemResult>.Fail(validationError);
        }

        var optionIds = request.ProductOptionIds.Distinct().ToArray();
        if (optionIds.Length != request.ProductOptionIds.Count)
        {
            return ApiResult<MenuItemResult>.Fail("Product option ids must be unique.");
        }

        var options = await _menus.ListProductOptionsAsync(productVariant.ProductId, optionIds, cancellationToken);
        if (options.Count != optionIds.Length)
        {
            return ApiResult<MenuItemResult>.Fail("Every product option must belong to the selected product.", 409);
        }

        var item = new MenuItem
        {
            MenuId = menu.Id,
            ProductId = productVariant.ProductId,
            ProductVariantId = request.ProductVariantId,
            RecipeId = request.RecipeId,
            Code = MenuNormalizer.NormalizeCode(request.Code),
            DisplayName = request.DisplayName.Trim(),
            Description = MenuNormalizer.TrimToNull(request.Description),
            Status = Domain.SalesCatalog.Enums.MenuItemStatus.Draft,
            Price = request.Price,
            DiscountAmount = request.DiscountAmount,
            Currency = menu.Currency,
            DisplayOrder = request.DisplayOrder,
            PreparationTimeSeconds = request.PreparationTimeSeconds,
            EffectiveFrom = request.EffectiveFrom,
            EffectiveTo = request.EffectiveTo,
            MetadataSchemaVersion = 1,
            CreatedAt = DateTimeOffset.UtcNow,
            CreatedByAccountId = createdByAccountId
        };

        foreach (var optionId in optionIds)
        {
            item.ProductOptions.Add(new MenuItemProductOption
            {
                ProductOptionId = optionId,
                CreatedAt = item.CreatedAt,
                CreatedByAccountId = createdByAccountId
            });
        }

        await _menus.AddMenuItemAsync(item, cancellationToken);
        await _menus.SaveChangesAsync(cancellationToken);

        return ApiResult<MenuItemResult>.Success(MenuResultMapper.ToItemResult(item), "Menu item created.", 201);
    }
}
