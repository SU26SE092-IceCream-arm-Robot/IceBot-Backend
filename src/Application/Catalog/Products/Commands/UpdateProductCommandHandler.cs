using Application.Catalog.Abstractions;
using Application.Catalog.Products.Mapping;
using Application.Catalog.Products.Results;
using Application.Catalog.Products.Rules;
using Application.Catalog.Products.Support;
using Application.Shared.Ownership;
using Application.Shared.Concurrency;
using Application.Shared.Wrappers;

namespace Application.Catalog.Products.Commands;

public sealed class UpdateProductCommandHandler
{
    private readonly IProductStore _products;
    private readonly ITechnicalResourceMutationPolicy _technicalOwnership;
    private readonly ITechnicalResourceMutationCoordinator _mutations;

    public UpdateProductCommandHandler(
        IProductStore products,
        ITechnicalResourceMutationPolicy technicalOwnership,
        ITechnicalResourceMutationCoordinator mutations)
    {
        _products = products;
        _technicalOwnership = technicalOwnership;
        _mutations = mutations;
    }

    public async Task<ApiResult<ProductResult>> HandleAsync(
        UpdateProductCommand command,
        CancellationToken cancellationToken = default) =>
        await _mutations.ExecuteAsync(
            [TechnicalResourceMutationIdentity.Product(command.ProductId)],
            ct => HandleLockedAsync(command, ct),
            cancellationToken);

    private async Task<ApiResult<ProductResult>> HandleLockedAsync(
        UpdateProductCommand command,
        CancellationToken cancellationToken)
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
        var newProductType = string.IsNullOrWhiteSpace(request.ProductType)
            ? product.ProductType
            : ProductNormalizer.NormalizeCode(request.ProductType);
        var newCurrency = string.IsNullOrWhiteSpace(request.Currency)
            ? product.Currency
            : ProductNormalizer.NormalizeCode(request.Currency);
        if (!string.Equals(newCode, product.Code, StringComparison.Ordinal) ||
            !string.Equals(newProductType, product.ProductType, StringComparison.Ordinal))
        {
            var ownershipError = await _technicalOwnership.ValidateDefinitionMutationAsync(
                TechnicalResourceKind.Product, product.Id, cancellationToken);
            if (ownershipError is not null) return ApiResult<ProductResult>.Fail(ownershipError, 409);
        }
        if (!string.Equals(newCurrency, product.Currency, StringComparison.Ordinal) &&
            await _products.IsProductReferencedByMenuItemsAsync(product.Id, cancellationToken))
        {
            return ApiResult<ProductResult>.Fail(
                "Product currency cannot change while the product is used by a menu item.",
                409);
        }

        var validationError = await ProductRequestValidator.ValidateProductFieldsAsync(
            _products,
            newCode,
            request.Name ?? product.Name,
            request.BasePrice ?? product.BasePrice,
            newCurrency,
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
        product.ProductType = newProductType;
        product.BasePrice = request.BasePrice ?? product.BasePrice;
        product.Currency = newCurrency;
        foreach (var variant in product.ProductVariants)
        {
            variant.Currency = product.Currency;
        }
        product.PreparationTimeSeconds = request.PreparationTimeSeconds ?? product.PreparationTimeSeconds;
        product.UpdatedAt = DateTimeOffset.UtcNow;
        product.UpdatedByAccountId = updatedByAccountId;

        await _products.SaveChangesAsync(cancellationToken);

        return ApiResult<ProductResult>.Success(ProductResultMapper.ToResult(product), "Product updated.");
    }
}
