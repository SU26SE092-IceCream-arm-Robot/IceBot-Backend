using Application.Catalog.Abstractions;
using Application.Catalog.Products.Mapping;
using Application.Catalog.Products.Results;
using Application.Shared.Wrappers;

namespace Application.Catalog.Products.Commands;

public sealed class SetProductVariantAvailabilityCommandHandler
{
    private readonly IProductStore _products;

    public SetProductVariantAvailabilityCommandHandler(IProductStore products)
    {
        _products = products;
    }

    public async Task<ApiResult<ProductVariantResult>> HandleAsync(
        SetProductVariantAvailabilityCommand command,
        CancellationToken cancellationToken = default)
    {
        var product = await _products.GetProductByIdAsync(command.ProductId, cancellationToken: cancellationToken);
        if (product is null)
        {
            return ApiResult<ProductVariantResult>.Fail("Product not found.", 404);
        }

        var accessError = ProductManagementCommandRules.ValidateExisting<ProductVariantResult>(command.Scope, product);
        if (accessError is not null)
        {
            return accessError;
        }

        var variant = await _products.GetProductVariantByIdAsync(command.ProductId, command.VariantId, asNoTracking: false, cancellationToken: cancellationToken);
        if (variant is null)
        {
            return ApiResult<ProductVariantResult>.Fail("Product variant not found.", 404);
        }

        variant.IsAvailable = command.IsAvailable;
        variant.UpdatedAt = DateTimeOffset.UtcNow;
        variant.UpdatedByAccountId = command.UpdatedByAccountId;
        await _products.SaveChangesAsync(cancellationToken);

        return ApiResult<ProductVariantResult>.Success(ProductResultMapper.ToVariantResult(variant), "Product variant availability updated.");
    }
}
