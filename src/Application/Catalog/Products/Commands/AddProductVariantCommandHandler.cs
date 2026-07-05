using Application.Catalog.Abstractions;
using Application.Catalog.Products.Mapping;
using Application.Catalog.Products.Results;
using Application.Catalog.Products.Rules;
using Application.Catalog.Products.Support;
using Application.Shared.Wrappers;

namespace Application.Catalog.Products.Commands;

public sealed class AddProductVariantCommandHandler
{
    private readonly IProductStore _products;

    public AddProductVariantCommandHandler(IProductStore products)
    {
        _products = products;
    }

    public async Task<ApiResult<ProductVariantResult>> HandleAsync(
        AddProductVariantCommand command,
        CancellationToken cancellationToken = default)
    {
        var productId = command.ProductId;
        var request = command.Request;
        var createdByAccountId = command.CreatedByAccountId;

        var product = await _products.GetProductByIdAsync(productId, asNoTracking: false, cancellationToken: cancellationToken);
        if (product is null)
        {
            return ApiResult<ProductVariantResult>.Fail("Product not found.", 404);
        }

        var accessError = ProductManagementCommandRules.ValidateExisting<ProductVariantResult>(command.Scope, product);
        if (accessError is not null)
        {
            return accessError;
        }

        var validationError = await ProductVariantRequestValidator.ValidateVariantFieldsAsync(
            _products,
            productId,
            request,
            product.Currency,
            null,
            cancellationToken);

        if (validationError is not null)
        {
            return ApiResult<ProductVariantResult>.Fail(validationError);
        }

        var variant = ProductVariantFactory.CreateVariant(
            request, product.Id, product.Currency, DateTimeOffset.UtcNow, createdByAccountId);
        await _products.AddProductVariantAsync(variant, cancellationToken);
        await _products.SaveChangesAsync(cancellationToken);

        return ApiResult<ProductVariantResult>.Success(ProductResultMapper.ToVariantResult(variant), "Product variant created.", 201);
    }
}
