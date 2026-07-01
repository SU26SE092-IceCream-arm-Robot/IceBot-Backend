using Application.SalesCatalog.Abstractions;
using Application.Shared.Wrappers;

namespace Application.SalesCatalog.Menus.Commands;

public sealed class DeleteMenuCommandHandler
{
    private readonly IMenuStore _menus;

    public DeleteMenuCommandHandler(IMenuStore menus)
    {
        _menus = menus;
    }

    public async Task<ApiResult<bool>> HandleAsync(
        DeleteMenuCommand command,
        CancellationToken cancellationToken = default)
    {
        var menu = await _menus.GetMenuByIdAsync(command.MenuId, asNoTracking: false, cancellationToken);
        if (menu is null)
        {
            return ApiResult<bool>.Fail("Menu not found.", 404);
        }

        var accessError = MenuManagementCommandRules.ValidateExisting<bool>(command.Scope, menu);
        if (accessError is not null)
        {
            return accessError;
        }

        var now = DateTimeOffset.UtcNow;
        menu.DeletedAt = now;
        menu.DeletedByAccountId = command.DeletedByAccountId;

        foreach (var item in menu.MenuItems)
        {
            item.DeletedAt = now;
            item.DeletedByAccountId = command.DeletedByAccountId;
        }

        await _menus.SaveChangesAsync(cancellationToken);
        return ApiResult<bool>.Success(true, "Menu deleted.");
    }
}
