using Application.Catalog.Abstractions;
using Application.Catalog.Products.Mapping;
using Application.Catalog.Products.Results;
using Application.Shared.Wrappers;

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

        var totalCount = await _products.CountProductsAsync(
            query.Search,
            query.OrganizationId,
            query.StoreId,
            query.KioskId,
            query.UserContext.IsSystemAdmin,
            query.UserContext.AllowedOrganizationIds,
            query.UserContext.AllowedStoreIds,
            query.UserContext.AllowedKioskIds,
            cancellationToken);

        var products = await _products.ListProductsAsync(
            query.Search,
            query.OrganizationId,
            query.StoreId,
            query.KioskId,
            query.UserContext.IsSystemAdmin,
            query.UserContext.AllowedOrganizationIds,
            query.UserContext.AllowedStoreIds,
            query.UserContext.AllowedKioskIds,
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
