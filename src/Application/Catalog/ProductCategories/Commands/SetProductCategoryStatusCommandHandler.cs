using Application.Catalog.Abstractions;
using Application.Catalog.ProductCategories.Mapping;
using Application.Catalog.ProductCategories.Results;
using Application.Shared.Wrappers;

namespace Application.Catalog.ProductCategories.Commands;

public sealed class SetProductCategoryStatusCommandHandler(ICatalogAuthoringStore catalog)
{
    public async Task<ApiResult<ProductCategoryResult>> HandleAsync(
        SetProductCategoryStatusCommand command,
        CancellationToken cancellationToken = default)
    {
        var category = await catalog.GetProductCategoryAsync(command.CategoryId, asNoTracking: false, cancellationToken);
        if (category is null)
        {
            return ApiResult<ProductCategoryResult>.Fail("Product category not found.", 404);
        }

        if (category.IsActive == command.IsActive)
        {
            return ApiResult<ProductCategoryResult>.Success(
                ProductCategoryResultMapper.ToResult(category),
                "Product category status is unchanged.");
        }

        category.IsActive = command.IsActive;
        category.UpdatedByAccountId = command.ActorId;
        await catalog.SaveChangesAsync(cancellationToken);

        return ApiResult<ProductCategoryResult>.Success(
            ProductCategoryResultMapper.ToResult(category),
            "Product category status updated.");
    }
}
