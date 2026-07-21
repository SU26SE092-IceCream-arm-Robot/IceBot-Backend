using Application.SalesCatalog.Abstractions;
using Application.SalesCatalog.Menus.Mapping;
using Application.SalesCatalog.Menus.Results;
using Application.SalesCatalog.Menus.Rules;
using Application.Shared.Wrappers;
using Domain.SalesCatalog.Enums;

namespace Application.SalesCatalog.Menus.Commands;

public sealed class SetMenuStatusCommandHandler
{
    private readonly IMenuStore _menus;

    public SetMenuStatusCommandHandler(IMenuStore menus)
    {
        _menus = menus;
    }

    public async Task<ApiResult<MenuResult>> HandleAsync(
        SetMenuStatusCommand command,
        CancellationToken cancellationToken = default)
    {
        var menu = await _menus.GetMenuByIdAsync(command.MenuId, asNoTracking: false, cancellationToken);
        if (menu is null)
        {
            return ApiResult<MenuResult>.Fail("Menu not found.", 404);
        }

        var accessError = MenuManagementCommandRules.ValidateExisting<MenuResult>(command.Scope, menu);
        if (accessError is not null)
        {
            return accessError;
        }

        if (command.Status == MenuStatus.Active)
        {
            foreach (var item in menu.MenuItems.Where(item => item.Status == MenuItemStatus.Active))
            {
                var activationError = await MenuItemActivationRules.ValidateAsync(
                    _menus, menu, item, cancellationToken);
                if (activationError is not null)
                    return ApiResult<MenuResult>.Fail(
                        $"Menu item '{item.DisplayName}' cannot be activated: {activationError}",
                        409);
            }
        }

        menu.Status = command.Status;
        menu.UpdatedAt = DateTimeOffset.UtcNow;
        menu.UpdatedByAccountId = command.UpdatedByAccountId;
        await _menus.SaveChangesAsync(cancellationToken);

        return ApiResult<MenuResult>.Success(MenuResultMapper.ToResult(menu), "Menu status updated.");
    }
}
