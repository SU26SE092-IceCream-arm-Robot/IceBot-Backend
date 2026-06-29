using Application.SalesCatalog.Abstractions;
using Application.SalesCatalog.Menus.Mapping;
using Application.SalesCatalog.Menus.Results;
using Application.Shared.Wrappers;
using Application.Tenants;

namespace Application.SalesCatalog.Menus.Queries;

public sealed class GetMenuQueryHandler
{
    private readonly IMenuStore _menus;

    public GetMenuQueryHandler(IMenuStore menus)
    {
        _menus = menus;
    }

    public async Task<ApiResult<MenuResult>> HandleAsync(
        GetMenuQuery query,
        CancellationToken cancellationToken = default)
    {
        var menu = await _menus.GetMenuByIdAsync(query.MenuId, cancellationToken: cancellationToken);
        if (menu is null)
        {
            return ApiResult<MenuResult>.Fail("Menu not found.", 404);
        }

        if (!ScopeAccessRules.CanAccessScopedRow(ScopeRoleSets.MenusManage,
            query.UserContext,
            menu.OrganizationId,
            menu.StoreId,
            menu.KioskId))
        {
            return ApiResult<MenuResult>.Fail("Access denied.", 403);
        }

        return ApiResult<MenuResult>.Success(MenuResultMapper.ToResult(menu));
    }
}
