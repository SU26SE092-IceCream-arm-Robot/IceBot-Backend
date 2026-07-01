using Application.Catalog.Abstractions;
using Application.Catalog.Products.Mapping;
using Application.Catalog.Products.Results;
using Application.Shared.Wrappers;

namespace Application.Catalog.Products.Commands;

public sealed class SetProductAvailabilityCommandHandler
{
    private readonly IProductStore _products;

    public SetProductAvailabilityCommandHandler(IProductStore products)
    {
        _products = products;
    }

    public async Task<ApiResult<ProductResult>> HandleAsync(
        SetProductAvailabilityCommand command,
        CancellationToken cancellationToken = default)
    {
        var product = await _products.GetProductByIdAsync(command.ProductId, asNoTracking: false, cancellationToken: cancellationToken);
        if (product is null)
        {
            return ApiResult<ProductResult>.Fail("Product not found.", 404);
        }

        var accessError = ProductManagementCommandRules.ValidateExisting<ProductResult>(command.Scope, product);
        if (accessError is not null)
        {
            return accessError;
        }

        product.IsAvailable = command.IsAvailable;
        product.UpdatedAt = DateTimeOffset.UtcNow;
        product.UpdatedByAccountId = command.UpdatedByAccountId;
        await _products.SaveChangesAsync(cancellationToken);

        return ApiResult<ProductResult>.Success(ProductResultMapper.ToResult(product), "Product availability updated.");
    }
}
