using Application.Catalog.Abstractions;
using Application.Shared.Wrappers;
using Application.Shared.Ownership;

namespace Application.Catalog.Products.Commands;

public sealed class DeleteProductCommandHandler
{
    private readonly IProductStore _products;
    private readonly ITechnicalResourceMutationPolicy _technicalOwnership;

    public DeleteProductCommandHandler(
        IProductStore products,
        ITechnicalResourceMutationPolicy technicalOwnership)
    {
        _products = products;
        _technicalOwnership = technicalOwnership;
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

        var accessError = ProductManagementCommandRules.ValidateExisting<bool>(command.Scope, product);
        if (accessError is not null)
        {
            return accessError;
        }

        var ownershipError = await _technicalOwnership.ValidateDefinitionMutationAsync(
            TechnicalResourceKind.Product, product.Id, cancellationToken);
        if (ownershipError is not null) return ApiResult<bool>.Fail(ownershipError, 409);

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
