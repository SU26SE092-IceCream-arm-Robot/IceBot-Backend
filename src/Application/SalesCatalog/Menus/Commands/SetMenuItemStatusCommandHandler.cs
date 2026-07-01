using Application.SalesCatalog.Abstractions;
using Application.SalesCatalog.Menus.Mapping;
using Application.SalesCatalog.Menus.Results;
using Application.Shared.Wrappers;

namespace Application.SalesCatalog.Menus.Commands;

public sealed class SetMenuItemStatusCommandHandler
{
    private readonly IMenuStore _menus;

    public SetMenuItemStatusCommandHandler(IMenuStore menus)
    {
        _menus = menus;
    }

    public async Task<ApiResult<MenuItemResult>> HandleAsync(
        SetMenuItemStatusCommand command,
        CancellationToken cancellationToken = default)
    {
        var menu = await _menus.GetMenuByIdAsync(command.MenuId, cancellationToken: cancellationToken);
        if (menu is null)
        {
            return ApiResult<MenuItemResult>.Fail("Menu not found.", 404);
        }

        var accessError = MenuManagementCommandRules.ValidateExisting<MenuItemResult>(command.Scope, menu);
        if (accessError is not null)
        {
            return accessError;
        }

        var item = await _menus.GetMenuItemByIdAsync(command.MenuId, command.MenuItemId, asNoTracking: false, cancellationToken);
        if (item is null)
        {
            return ApiResult<MenuItemResult>.Fail("Menu item not found.", 404);
        }

        item.Status = command.Status;
        item.UpdatedAt = DateTimeOffset.UtcNow;
        item.UpdatedByAccountId = command.UpdatedByAccountId;
        await _menus.SaveChangesAsync(cancellationToken);

        return ApiResult<MenuItemResult>.Success(MenuResultMapper.ToItemResult(item), "Menu item status updated.");
    }
}
