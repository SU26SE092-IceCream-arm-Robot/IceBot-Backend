using Application.Catalog.Abstractions;
using Application.Catalog.Products.Mapping;
using Application.Catalog.Products.Results;
using Application.Catalog.Products.Rules;
using Application.Catalog.Products.Support;
using Application.Shared.Wrappers;
using Application.Shared.Ownership;

namespace Application.Catalog.Products.Commands;

public sealed class UpdateProductVariantCommandHandler
{
    private readonly IProductStore _products;
    private readonly ITechnicalResourceMutationPolicy _technicalOwnership;

    public UpdateProductVariantCommandHandler(
        IProductStore products,
        ITechnicalResourceMutationPolicy technicalOwnership)
    {
        _products = products;
        _technicalOwnership = technicalOwnership;
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
        var newVariantType = string.IsNullOrWhiteSpace(request.VariantType)
            ? variant.VariantType
            : ProductNormalizer.NormalizeCode(request.VariantType);
        var newSizeCode = request.SizeCode is null
            ? variant.SizeCode
            : ProductNormalizer.NormalizeNullableCode(request.SizeCode);
        if (!string.Equals(newCode, variant.Code, StringComparison.Ordinal) ||
            !string.Equals(newVariantType, variant.VariantType, StringComparison.Ordinal) ||
            !string.Equals(newSizeCode, variant.SizeCode, StringComparison.Ordinal) ||
            request.FulfillmentType.HasValue && request.FulfillmentType.Value != variant.FulfillmentType)
        {
            var ownershipError = await _technicalOwnership.ValidateDefinitionMutationAsync(
                TechnicalResourceKind.ProductVariant, variant.Id, cancellationToken);
            if (ownershipError is not null) return ApiResult<ProductVariantResult>.Fail(ownershipError, 409);
        }

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
        variant.VariantType = newVariantType;
        variant.FulfillmentType = request.FulfillmentType ?? variant.FulfillmentType;
        variant.SizeCode = newSizeCode;
        variant.BasePrice = request.BasePrice ?? variant.BasePrice;
        variant.Currency = product.Currency;
        variant.DisplayOrder = request.DisplayOrder ?? variant.DisplayOrder;
        variant.PreparationTimeSeconds = request.PreparationTimeSeconds ?? variant.PreparationTimeSeconds;
        variant.ImageUrl = request.ImageUrl is null ? variant.ImageUrl : ProductNormalizer.TrimToNull(request.ImageUrl);
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
