using Application.Catalog.Abstractions;
using Application.Shared.Wrappers;
using Application.Shared.Ownership;
using Application.Shared.Concurrency;

namespace Application.Catalog.Products.Commands;

public sealed class DeleteProductVariantCommandHandler
{
    private readonly IProductStore _products;
    private readonly ITechnicalResourceMutationPolicy _technicalOwnership;
    private readonly ITechnicalResourceMutationCoordinator _mutations;

    public DeleteProductVariantCommandHandler(
        IProductStore products,
        ITechnicalResourceMutationPolicy technicalOwnership,
        ITechnicalResourceMutationCoordinator mutations)
    {
        _products = products;
        _technicalOwnership = technicalOwnership;
        _mutations = mutations;
    }

    public async Task<ApiResult<bool>> HandleAsync(
        DeleteProductVariantCommand command,
        CancellationToken cancellationToken = default) =>
        await _mutations.ExecuteAsync(
            [TechnicalResourceMutationIdentity.Product(command.ProductId)],
            ct => HandleLockedAsync(command, ct),
            cancellationToken);

    private async Task<ApiResult<bool>> HandleLockedAsync(
        DeleteProductVariantCommand command,
        CancellationToken cancellationToken)
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

        var ownershipError = await _technicalOwnership.ValidateDefinitionMutationAsync(
            TechnicalResourceKind.ProductVariant, variant.Id, cancellationToken);
        if (ownershipError is not null) return ApiResult<bool>.Fail(ownershipError, 409);
        if (await _products.IsProductVariantReferencedByMenuItemsAsync(variant.Id, cancellationToken))
            return ApiResult<bool>.Fail(
                "Product variant is used by one or more menu items. Archive or replace those menu items before deleting it.",
                409);

        variant.DeletedAt = DateTimeOffset.UtcNow;
        variant.DeletedByAccountId = command.DeletedByAccountId;
        await _products.SaveChangesAsync(cancellationToken);

        return ApiResult<bool>.Success(true, "Product variant deleted.");
    }
}
