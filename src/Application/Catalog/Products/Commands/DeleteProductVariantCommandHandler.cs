using Application.Catalog.Abstractions;
using Application.Shared.Wrappers;

namespace Application.Catalog.Products.Commands;

public sealed class DeleteProductVariantCommandHandler
{
    private readonly IProductStore _products;

    public DeleteProductVariantCommandHandler(IProductStore products)
    {
        _products = products;
    }

    public async Task<ApiResult<bool>> HandleAsync(
        DeleteProductVariantCommand command,
        CancellationToken cancellationToken = default)
    {
        var product = await _products.GetProductByIdAsync(command.ProductId, cancellationToken: cancellationToken);
        if (product is null)
        {
            return ApiResult<bool>.Fail("Product not found.", 404);
        }

        var accessError = ProductManagementCommandRules.ValidateExisting<bool>(command.Scope, product);
        if (accessError is not null)
        {
            return accessError;
        }

        var variant = await _products.GetProductVariantByIdAsync(command.ProductId, command.VariantId, asNoTracking: false, cancellationToken: cancellationToken);
        if (variant is null)
        {
            return ApiResult<bool>.Fail("Product variant not found.", 404);
        }

        variant.DeletedAt = DateTimeOffset.UtcNow;
        variant.DeletedByAccountId = command.DeletedByAccountId;
        await _products.SaveChangesAsync(cancellationToken);

        return ApiResult<bool>.Success(true, "Product variant deleted.");
    }
}
