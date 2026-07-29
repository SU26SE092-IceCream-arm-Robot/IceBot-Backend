using Application.SalesCatalog.Abstractions;
using Application.SalesCatalog.Menus.Mapping;
using Application.SalesCatalog.Menus.Results;
using Application.SalesCatalog.Menus.Rules;
using Application.SalesCatalog.Menus.Support;
using Application.Shared.Wrappers;
using Application.Shared.Concurrency;

namespace Application.SalesCatalog.Menus.Commands;

public sealed class UpdateMenuCommandHandler
{
    private readonly IMenuStore _menus;
    private readonly ITechnicalResourceMutationCoordinator _mutations;

    public UpdateMenuCommandHandler(
        IMenuStore menus,
        ITechnicalResourceMutationCoordinator mutations)
    {
        _menus = menus;
        _mutations = mutations;
    }

    public async Task<ApiResult<MenuResult>> HandleAsync(
        UpdateMenuCommand command,
        CancellationToken cancellationToken = default) =>
        await _mutations.ExecuteAsync(
            [TechnicalResourceMutationIdentity.Menu(command.MenuId)],
            ct => HandleLockedAsync(command, ct),
            cancellationToken);

    private async Task<ApiResult<MenuResult>> HandleLockedAsync(
        UpdateMenuCommand command,
        CancellationToken cancellationToken)
    {
        var menuId = command.MenuId;
        var request = command.Request;
        var updatedByAccountId = command.UpdatedByAccountId;

        var menu = await _menus.GetMenuByIdAsync(menuId, asNoTracking: false, cancellationToken);
        if (menu is null)
        {
            return ApiResult<MenuResult>.Fail("Menu not found.", 404);
        }

        var accessError = MenuManagementCommandRules.ValidateExisting<MenuResult>(command.Scope, menu);
        if (accessError is not null)
        {
            return accessError;
        }

        var newCode = string.IsNullOrWhiteSpace(request.Code) ? menu.Code : MenuNormalizer.NormalizeCode(request.Code);
        var newName = string.IsNullOrWhiteSpace(request.Name) ? menu.Name : request.Name;
        var newCurrency = string.IsNullOrWhiteSpace(request.Currency) ? menu.Currency : request.Currency;
        var newEffectiveFrom = request.EffectiveFrom ?? menu.EffectiveFrom;
        var newEffectiveTo = request.EffectiveTo ?? menu.EffectiveTo;

        if (!string.Equals(
                MenuNormalizer.NormalizeCode(newCurrency),
                menu.Currency,
                StringComparison.Ordinal) &&
            menu.MenuItems.Count != 0)
        {
            return ApiResult<MenuResult>.Fail(
                "Menu currency cannot change while the menu contains items.",
                409);
        }

        var validationError = await MenuRequestValidator.ValidateMenuFieldsAsync(
            _menus,
            newCode,
            newName,
            newCurrency,
            menu.ScopeType,
            menu.OrganizationId,
            menu.StoreId,
            menu.KioskId,
            newEffectiveFrom,
            newEffectiveTo,
            menu.Id,
            cancellationToken);

        if (validationError is not null)
        {
            return ApiResult<MenuResult>.Fail(validationError);
        }

        menu.Code = newCode;
        menu.Name = newName.Trim();
        menu.Description = request.Description is null ? menu.Description : MenuNormalizer.TrimToNull(request.Description);
        menu.Currency = MenuNormalizer.NormalizeCode(newCurrency);
        menu.EffectiveFrom = newEffectiveFrom;
        menu.EffectiveTo = newEffectiveTo;
        menu.DisplayOrder = request.DisplayOrder ?? menu.DisplayOrder;
        menu.UpdatedAt = DateTimeOffset.UtcNow;
        menu.UpdatedByAccountId = updatedByAccountId;

        await _menus.SaveChangesAsync(cancellationToken);
        return ApiResult<MenuResult>.Success(MenuResultMapper.ToResult(menu), "Menu updated.");
    }
}
