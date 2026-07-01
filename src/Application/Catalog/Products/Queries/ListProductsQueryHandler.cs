using Application.Catalog.Abstractions;
using Application.Catalog.Products.Mapping;
using Application.Catalog.Products.Results;
using Application.Shared.Wrappers;
using Application.Tenants;

namespace Application.Catalog.Products.Queries;

public sealed class ListProductsQueryHandler
{
    private readonly IProductStore _products;

    public ListProductsQueryHandler(IProductStore products)
    {
        _products = products;
    }

    public async Task<PagedResult<ProductResult>> HandleAsync(
        ListProductsQuery query,
        CancellationToken cancellationToken = default)
    {
        var pageNumber = Math.Max(query.PageNumber, 1);
        var pageSize = Math.Clamp(query.PageSize, 1, 100);
        var effectiveScope = ScopeAccessRules.GetEffectiveScope(ScopeRoleSets.ProductsManage, query.UserContext);
        var canReadGlobalTemplates = query.UserContext.IsSystemAdmin || query.UserContext.RoleScopes.Any(scope =>
            ScopeRoleSets.ProductTemplatesRead.Contains(scope.RoleCode, StringComparer.OrdinalIgnoreCase));

        var totalCount = await _products.CountProductsAsync(
            query.Search,
            query.OrganizationId,
            query.StoreId,
            query.KioskId,
            query.GlobalTemplatesOnly,
            query.UserContext.IsSystemAdmin || (query.GlobalTemplatesOnly && canReadGlobalTemplates),
            effectiveScope.OrganizationIds,
            effectiveScope.StoreIds,
            effectiveScope.KioskIds,
            cancellationToken);

        var products = await _products.ListProductsAsync(
            query.Search,
            query.OrganizationId,
            query.StoreId,
            query.KioskId,
            query.GlobalTemplatesOnly,
            query.UserContext.IsSystemAdmin || (query.GlobalTemplatesOnly && canReadGlobalTemplates),
            effectiveScope.OrganizationIds,
            effectiveScope.StoreIds,
            effectiveScope.KioskIds,
            pageNumber,
            pageSize,
            cancellationToken);

        return PagedResult<ProductResult>.Success(
            products.Select(ProductResultMapper.ToResult),
            totalCount,
            pageNumber,
            pageSize);
    }
}
