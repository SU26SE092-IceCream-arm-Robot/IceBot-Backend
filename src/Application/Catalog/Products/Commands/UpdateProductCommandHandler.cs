using Application.Catalog.Abstractions;
using Application.Catalog.Products.Mapping;
using Application.Catalog.Products.Results;
using Application.Catalog.Products.Rules;
using Application.Catalog.Products.Support;
using Application.Shared.Wrappers;

namespace Application.Catalog.Products.Commands;

public sealed class UpdateProductCommandHandler
{
    private readonly IProductStore _products;

    public UpdateProductCommandHandler(IProductStore products)
    {
        _products = products;
    }

    public async Task<ApiResult<ProductResult>> HandleAsync(
        UpdateProductCommand command,
        CancellationToken cancellationToken = default)
    {
        var productId = command.ProductId;
        var request = command.Request;
        var updatedByAccountId = command.UpdatedByAccountId;

        var product = await _products.GetProductByIdAsync(productId, asNoTracking: false, cancellationToken: cancellationToken);
        if (product is null)
        {
            return ApiResult<ProductResult>.Fail("Product not found.", 404);
        }

        var accessError = ProductManagementCommandRules.ValidateExisting<ProductResult>(command.Scope, product);
        if (accessError is not null)
        {
            return accessError;
        }

        var newCode = string.IsNullOrWhiteSpace(request.Code) ? product.Code : ProductNormalizer.NormalizeCode(request.Code);

        var validationError = await ProductRequestValidator.ValidateProductFieldsAsync(
            _products,
            newCode,
            request.Name ?? product.Name,
            request.BasePrice ?? product.BasePrice,
            request.Currency ?? product.Currency,
            request.PreparationTimeSeconds ?? product.PreparationTimeSeconds,
            product.ScopeType,
            product.OrganizationId,
            product.StoreId,
            product.KioskId,
            request.CategoryId ?? product.CategoryId,
            productId,
            cancellationToken);

        if (validationError is not null)
        {
            return ApiResult<ProductResult>.Fail(validationError);
        }

        product.CategoryId = request.CategoryId ?? product.CategoryId;
        product.Code = newCode;
        product.Name = string.IsNullOrWhiteSpace(request.Name) ? product.Name : request.Name.Trim();
        product.DisplayName = request.DisplayName is null ? product.DisplayName : ProductNormalizer.TrimToNull(request.DisplayName);
        product.Description = request.Description is null ? product.Description : ProductNormalizer.TrimToNull(request.Description);
        product.ProductType = string.IsNullOrWhiteSpace(request.ProductType)
            ? product.ProductType
            : ProductNormalizer.NormalizeCode(request.ProductType);
        product.BasePrice = request.BasePrice ?? product.BasePrice;
        product.Currency = string.IsNullOrWhiteSpace(request.Currency)
            ? product.Currency
            : ProductNormalizer.NormalizeCode(request.Currency);
        product.IsAvailable = request.IsAvailable ?? product.IsAvailable;
        product.PreparationTimeSeconds = request.PreparationTimeSeconds ?? product.PreparationTimeSeconds;
        product.ImageUrl = request.ImageUrl is null ? product.ImageUrl : ProductNormalizer.TrimToNull(request.ImageUrl);
        product.MetadataJson = request.MetadataJson is null ? product.MetadataJson : ProductNormalizer.TrimToNull(request.MetadataJson);
        product.UpdatedAt = DateTimeOffset.UtcNow;
        product.UpdatedByAccountId = updatedByAccountId;

        await _products.SaveChangesAsync(cancellationToken);

        return ApiResult<ProductResult>.Success(ProductResultMapper.ToResult(product), "Product updated.");
    }
}
