using Application.Catalog.Abstractions;
using Application.Catalog.Products.Mapping;
using Application.Catalog.Products.Results;
using Application.Shared.Wrappers;
using Application.Tenants;
using Domain.Tenants.Enums;

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

        if (query.IsGlobalTemplate)
        {
            var canReadTemplates = query.UserContext.IsSystemAdmin || query.UserContext.RoleScopes.Any(scope =>
                ScopeRoleSets.ProductTemplatesRead.Contains(scope.RoleCode, StringComparer.OrdinalIgnoreCase));
            if (!canReadTemplates || product.ScopeType != TenantScopeType.Global || product.OrganizationId is not null)
            {
                return ApiResult<ProductResult>.Fail("Product template not found.", 404);
            }
        }
        else if (!query.OrganizationId.HasValue ||
                 product.OrganizationId != query.OrganizationId ||
                 product.ScopeType == TenantScopeType.Global ||
                 !ScopeAccessRules.CanAccessScopedRow(ScopeRoleSets.ProductsManage,
                     query.UserContext,
                     product.OrganizationId,
                     product.StoreId,
                     product.KioskId))
        {
            return ApiResult<ProductResult>.Fail("Product not found.", 404);
        }

        return ApiResult<ProductResult>.Success(ProductResultMapper.ToResult(product));
    }
}
