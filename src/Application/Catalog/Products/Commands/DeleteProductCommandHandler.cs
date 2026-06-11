using Application.Catalog.Abstractions;
using Application.Shared.Wrappers;

namespace Application.Catalog.Products.Commands;

public sealed class DeleteProductCommandHandler
{
    private readonly IProductStore _products;

    public DeleteProductCommandHandler(IProductStore products)
    {
        _products = products;
    }

    public async Task<ApiResult<bool>> HandleAsync(
        DeleteProductCommand command,
        CancellationToken cancellationToken = default)
    {
        var product = await _products.GetProductByIdAsync(command.ProductId, asNoTracking: false, cancellationToken: cancellationToken);
        if (product is null)
        {
            return ApiResult<bool>.Fail("Product not found.", 404);
        }

        var now = DateTimeOffset.UtcNow;
        product.DeletedAt = now;
        product.DeletedByAccountId = command.DeletedByAccountId;

        foreach (var variant in product.ProductVariants)
        {
            variant.DeletedAt = now;
            variant.DeletedByAccountId = command.DeletedByAccountId;
        }

        await _products.SaveChangesAsync(cancellationToken);
        return ApiResult<bool>.Success(true, "Product deleted.");
    }
}
