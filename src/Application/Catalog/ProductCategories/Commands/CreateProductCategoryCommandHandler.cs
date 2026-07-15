using Application.Catalog.Abstractions;
using Application.Catalog.ProductCategories.Mapping;
using Application.Catalog.ProductCategories.Results;
using Application.Catalog.Products.Support;
using Application.Shared.Wrappers;
using Domain.Catalog.Entities;

namespace Application.Catalog.ProductCategories.Commands;

public sealed class CreateProductCategoryCommandHandler(ICatalogAuthoringStore catalog)
{
    public async Task<ApiResult<ProductCategoryResult>> HandleAsync(
        CreateProductCategoryCommand command,
        CancellationToken cancellationToken = default)
    {
        var request = command.Request;
        var code = ProductNormalizer.NormalizeCode(request.Code);
        if (await catalog.ProductCategoryCodeExistsAsync(code, cancellationToken: cancellationToken))
        {
            return ApiResult<ProductCategoryResult>.Fail("Product category code already exists.", 409);
        }

        var category = new ProductCategory
        {
            Code = code,
            Name = request.Name.Trim(),
            Description = ProductNormalizer.TrimToNull(request.Description),
            ProductType = request.ProductType.Trim(),
            ImageUrl = ProductNormalizer.TrimToNull(request.ImageUrl),
            DisplayOrder = request.DisplayOrder,
            IsActive = true,
            CreatedByAccountId = command.ActorId
        };

        await catalog.AddProductCategoryAsync(category, cancellationToken);
        if (!await catalog.TrySaveChangesAsync(cancellationToken))
        {
            return ApiResult<ProductCategoryResult>.Fail("Product category code already exists.", 409);
        }

        return ApiResult<ProductCategoryResult>.Success(ProductCategoryResultMapper.ToResult(category), "Product category created.", 201);
    }
}
