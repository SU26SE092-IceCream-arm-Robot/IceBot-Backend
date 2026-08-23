using Application.Catalog.Abstractions;
using Application.Catalog.Products.Mapping;
using Application.Catalog.Products.Results;
using Application.Shared.Concurrency;
using Application.Shared.Wrappers;

namespace Application.Catalog.Products.Commands;

public sealed class SetProductAvailabilityCommandHandler
{
    private readonly IProductStore _products;
    private readonly ITechnicalResourceMutationCoordinator _mutations;

    public SetProductAvailabilityCommandHandler(
        IProductStore products,
        ITechnicalResourceMutationCoordinator mutations)
    {
        _products = products;
        _mutations = mutations;
    }

    public async Task<ApiResult<ProductResult>> HandleAsync(
        SetProductAvailabilityCommand command,
        CancellationToken cancellationToken = default)
        => await _mutations.ExecuteAsync(
            [TechnicalResourceMutationIdentity.Product(command.ProductId)],
            ct => HandleLockedAsync(command, ct),
            cancellationToken);

    private async Task<ApiResult<ProductResult>> HandleLockedAsync(
        SetProductAvailabilityCommand command,
        CancellationToken cancellationToken)
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
