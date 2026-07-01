using Application.SalesCatalog.Abstractions;
using Application.Shared.Wrappers;

namespace Application.SalesCatalog.Menus.Commands;

public sealed class DeleteMenuItemCommandHandler
{
    private readonly IMenuStore _menus;

    public DeleteMenuItemCommandHandler(IMenuStore menus)
    {
        _menus = menus;
    }

    public async Task<ApiResult<bool>> HandleAsync(
        DeleteMenuItemCommand command,
        CancellationToken cancellationToken = default)
    {
        var menu = await _menus.GetMenuByIdAsync(command.MenuId, cancellationToken: cancellationToken);
        if (menu is null)
        {
            return ApiResult<bool>.Fail("Menu not found.", 404);
        }

        var accessError = MenuManagementCommandRules.ValidateExisting<bool>(command.Scope, menu);
        if (accessError is not null)
        {
            return accessError;
        }

        var item = await _menus.GetMenuItemByIdAsync(command.MenuId, command.MenuItemId, asNoTracking: false, cancellationToken);
        if (item is null)
        {
            return ApiResult<bool>.Fail("Menu item not found.", 404);
        }

        item.DeletedAt = DateTimeOffset.UtcNow;
        item.DeletedByAccountId = command.DeletedByAccountId;
        await _menus.SaveChangesAsync(cancellationToken);

        return ApiResult<bool>.Success(true, "Menu item deleted.");
    }
}
