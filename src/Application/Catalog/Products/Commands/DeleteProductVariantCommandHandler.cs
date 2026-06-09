using Application.Catalog.Abstractions;
using Application.Shared.Wrappers;
using System;
using System.Threading;
using System.Threading.Tasks;

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
