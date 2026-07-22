using Application.Catalog.Abstractions;
using Application.Catalog.ProductCategories.Mapping;
using Application.Catalog.ProductCategories.Results;
using Application.Shared.Wrappers;

namespace Application.Catalog.ProductCategories.Queries;

public sealed class ListProductCategoriesQueryHandler(ICatalogAuthoringStore catalog)
{
    public async Task<ApiResult<List<ProductCategoryResult>>> HandleAsync(
        ListProductCategoriesQuery query,
        CancellationToken cancellationToken = default)
    {
        var categories = await catalog.ListProductCategoriesAsync(query.IncludeInactive, cancellationToken);
        return ApiResult<List<ProductCategoryResult>>.Success(
            categories.Select(ProductCategoryResultMapper.ToResult).ToList());
    }
}
