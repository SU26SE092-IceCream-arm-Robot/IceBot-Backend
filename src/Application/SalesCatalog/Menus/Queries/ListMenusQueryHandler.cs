using Application.SalesCatalog.Abstractions;
using Application.SalesCatalog.Menus.Mapping;
using Application.SalesCatalog.Menus.Results;
using Application.Shared.Wrappers;
using Application.Tenants;

namespace Application.SalesCatalog.Menus.Queries;

public sealed class ListMenusQueryHandler
{
    private readonly IMenuStore _menus;

    public ListMenusQueryHandler(IMenuStore menus)
    {
        _menus = menus;
    }

    public async Task<PagedResult<MenuResult>> HandleAsync(
        ListMenusQuery query,
        CancellationToken cancellationToken = default)
    {
        var pageNumber = Math.Max(query.PageNumber, 1);
        var pageSize = Math.Clamp(query.PageSize, 1, 100);
        var effectiveScope = ScopeAccessRules.GetEffectiveScope(ScopeRoleSets.MenusManage, query.UserContext);

        var totalCount = await _menus.CountMenusAsync(
            query.Search,
            query.OrganizationId,
            query.StoreId,
            query.KioskId,
            query.UserContext.IsSystemAdmin,
            effectiveScope.OrganizationIds,
            effectiveScope.StoreIds,
            effectiveScope.KioskIds,
            cancellationToken);

        var menus = await _menus.ListMenusAsync(
            query.Search,
            query.OrganizationId,
            query.StoreId,
            query.KioskId,
            query.UserContext.IsSystemAdmin,
            effectiveScope.OrganizationIds,
            effectiveScope.StoreIds,
            effectiveScope.KioskIds,
            pageNumber,
            pageSize,
            cancellationToken);

        return PagedResult<MenuResult>.Success(
            menus.Select(MenuResultMapper.ToResult),
            totalCount,
            pageNumber,
            pageSize);
    }
}
