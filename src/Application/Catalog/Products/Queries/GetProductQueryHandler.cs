using Application.Catalog.Abstractions;
using Application.Catalog.Products.Mapping;
using Application.Catalog.Products.Results;
using Application.Shared.Wrappers;
using Application.Tenants;

namespace Application.Catalog.Products.Queries;

public sealed class GetProductQueryHandler
{
    private readonly IProductStore _products;

    public GetProductQueryHandler(IProductStore products)
    {
        _products = products;
    }

    public async Task<ApiResult<ProductResult>> HandleAsync(
        GetProductQuery query,
        CancellationToken cancellationToken = default)
    {
        var product = await _products.GetProductByIdAsync(query.ProductId, cancellationToken: cancellationToken);
        if (product is null)
        {
            return ApiResult<ProductResult>.Fail("Product not found.", 404);
        }

        if (!ScopeAccessRules.CanAccessScopedRow(ScopeRoleSets.ProductsManage,
            query.UserContext,
            product.OrganizationId,
            product.StoreId,
            product.KioskId))
        {
            return ApiResult<ProductResult>.Fail("Access denied.", 403);
        }

        return ApiResult<ProductResult>.Success(ProductResultMapper.ToResult(product));
    }
}
