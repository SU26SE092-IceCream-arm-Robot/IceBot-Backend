using Application.Catalog.Abstractions;
using Application.Shared.Wrappers;

namespace Application.Catalog.ProductCategories.Commands;

public sealed class DeleteProductCategoryCommandHandler(ICatalogAuthoringStore catalog)
{
    public async Task<ApiResult<bool>> HandleAsync(
        DeleteProductCategoryCommand command,
        CancellationToken cancellationToken = default)
    {
        var category = await catalog.GetProductCategoryAsync(command.CategoryId, asNoTracking: false, cancellationToken);
        if (category is null)
        {
            return ApiResult<bool>.Fail("Product category not found.", 404);
        }

        if (await catalog.IsProductCategoryReferencedAsync(category.Id, cancellationToken))
        {
            return ApiResult<bool>.Fail("Product category is referenced by catalog data and cannot be deleted.", 409);
        }

        catalog.RemoveProductCategory(category);
        await catalog.SaveChangesAsync(cancellationToken);
        return ApiResult<bool>.Success(true, "Product category deleted.");
    }
}
