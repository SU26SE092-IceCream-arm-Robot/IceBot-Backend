using Application.Catalog.Abstractions;
using Application.Catalog.ProductCategories.Mapping;
using Application.Catalog.ProductCategories.Results;
using Application.Catalog.Products.Support;
using Application.Shared.Wrappers;

namespace Application.Catalog.ProductCategories.Commands;

public sealed class UpdateProductCategoryCommandHandler(ICatalogAuthoringStore catalog)
{
    public async Task<ApiResult<ProductCategoryResult>> HandleAsync(
        UpdateProductCategoryCommand command,
        CancellationToken cancellationToken = default)
    {
        var category = await catalog.GetProductCategoryAsync(command.CategoryId, asNoTracking: false, cancellationToken);
        if (category is null)
        {
            return ApiResult<ProductCategoryResult>.Fail("Product category not found.", 404);
        }

        var request = command.Request;
        category.Name = request.Name.Trim();
        category.Description = ProductNormalizer.TrimToNull(request.Description);
        category.ProductType = request.ProductType.Trim();
        category.ImageUrl = ProductNormalizer.TrimToNull(request.ImageUrl);
        category.DisplayOrder = request.DisplayOrder;
        category.UpdatedByAccountId = command.ActorId;

        await catalog.SaveChangesAsync(cancellationToken);
        return ApiResult<ProductCategoryResult>.Success(ProductCategoryResultMapper.ToResult(category), "Product category updated.");
    }
}
