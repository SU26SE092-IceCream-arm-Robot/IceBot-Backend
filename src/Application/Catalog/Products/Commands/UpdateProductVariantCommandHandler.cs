using Application.Catalog.Abstractions;
using Application.Catalog.Products.Mapping;
using Application.Catalog.Products.Results;
using Application.Catalog.Products.Rules;
using Application.Catalog.Products.Support;
using Application.Shared.Wrappers;

namespace Application.Catalog.Products.Commands;

public sealed class UpdateProductVariantCommandHandler
{
    private readonly IProductStore _products;

    public UpdateProductVariantCommandHandler(IProductStore products)
    {
        _products = products;
    }

    public async Task<ApiResult<ProductVariantResult>> HandleAsync(
        UpdateProductVariantCommand command,
        CancellationToken cancellationToken = default)
    {
        var productId = command.ProductId;
        var variantId = command.VariantId;
        var request = command.Request;
        var updatedByAccountId = command.UpdatedByAccountId;

        var product = await _products.GetProductByIdAsync(productId, cancellationToken: cancellationToken);
        if (product is null)
        {
            return ApiResult<ProductVariantResult>.Fail("Product not found.", 404);
        }

        var accessError = ProductManagementCommandRules.ValidateExisting<ProductVariantResult>(command.Scope, product);
        if (accessError is not null)
        {
            return accessError;
        }

        var variant = await _products.GetProductVariantByIdAsync(productId, variantId, asNoTracking: false, cancellationToken: cancellationToken);
        if (variant is null)
        {
            return ApiResult<ProductVariantResult>.Fail("Product variant not found.", 404);
        }

        var newCode = string.IsNullOrWhiteSpace(request.Code) ? variant.Code : ProductNormalizer.NormalizeCode(request.Code);
        if (await _products.ProductVariantCodeExistsAsync(productId, newCode, variantId, cancellationToken))
        {
            return ApiResult<ProductVariantResult>.Fail("Product variant code already exists for this product.", 409);
        }

        if (!string.IsNullOrWhiteSpace(request.Name))
        {
            variant.Name = request.Name.Trim();
        }

        variant.Code = newCode;
        variant.DisplayName = request.DisplayName is null ? variant.DisplayName : ProductNormalizer.TrimToNull(request.DisplayName);
        variant.Description = request.Description is null ? variant.Description : ProductNormalizer.TrimToNull(request.Description);
        variant.VariantType = string.IsNullOrWhiteSpace(request.VariantType)
            ? variant.VariantType
            : ProductNormalizer.NormalizeCode(request.VariantType);
        variant.FulfillmentType = request.FulfillmentType ?? variant.FulfillmentType;
        variant.SizeCode = request.SizeCode is null ? variant.SizeCode : ProductNormalizer.NormalizeNullableCode(request.SizeCode);
        variant.BasePrice = request.BasePrice ?? variant.BasePrice;
        variant.Currency = string.IsNullOrWhiteSpace(request.Currency)
            ? variant.Currency
            : ProductNormalizer.NormalizeCode(request.Currency);
        variant.IsAvailable = request.IsAvailable ?? variant.IsAvailable;
        variant.DisplayOrder = request.DisplayOrder ?? variant.DisplayOrder;
        variant.PreparationTimeSeconds = request.PreparationTimeSeconds ?? variant.PreparationTimeSeconds;
        variant.ImageUrl = request.ImageUrl is null ? variant.ImageUrl : ProductNormalizer.TrimToNull(request.ImageUrl);
        variant.MetadataJson = request.MetadataJson is null ? variant.MetadataJson : ProductNormalizer.TrimToNull(request.MetadataJson);
        variant.UpdatedAt = DateTimeOffset.UtcNow;
        variant.UpdatedByAccountId = updatedByAccountId;

        var validationError = ProductVariantRequestValidator.ValidateVariantValues(
            variant.Code,
            variant.Name,
            variant.BasePrice,
            variant.Currency,
            variant.PreparationTimeSeconds,
            variant.FulfillmentType);

        if (validationError is not null)
        {
            return ApiResult<ProductVariantResult>.Fail(validationError);
        }

        await _products.SaveChangesAsync(cancellationToken);
        return ApiResult<ProductVariantResult>.Success(ProductResultMapper.ToVariantResult(variant), "Product variant updated.");
    }
}
