using Application.Catalog.Abstractions;
using Application.Shared.Wrappers;
using Application.Shared.Ownership;
using Application.Shared.Concurrency;
using Domain.Catalog.Entities;

namespace Application.Catalog.Products.Commands;

public sealed class DeleteProductCommandHandler
{
    private readonly IProductStore _products;
    private readonly ITechnicalResourceMutationPolicy _technicalOwnership;
    private readonly ITechnicalResourceMutationCoordinator _mutations;

    public DeleteProductCommandHandler(
        IProductStore products,
        ITechnicalResourceMutationPolicy technicalOwnership,
        ITechnicalResourceMutationCoordinator mutations)
    {
        _products = products;
        _technicalOwnership = technicalOwnership;
        _mutations = mutations;
    }

    public async Task<ApiResult<bool>> HandleAsync(
        DeleteProductCommand command,
        CancellationToken cancellationToken = default) =>
        await _mutations.ExecuteAsync(
            [TechnicalResourceMutationIdentity.Product(command.ProductId)],
            ct => HandleLockedAsync(command, ct),
            cancellationToken);

    private async Task<ApiResult<bool>> HandleLockedAsync(
        DeleteProductCommand command,
        CancellationToken cancellationToken)
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
        if (await _products.IsProductReferencedByMenuItemsAsync(product.Id, cancellationToken))
            return ApiResult<bool>.Fail(
                "Product is used by one or more menu items. Archive or replace those menu items before deleting it.",
                409);

        var now = DateTimeOffset.UtcNow;
        var imageAssetsToClean = product.ProductVariants
            .Select(variant => variant.ImageAsset)
            .Append(product.ImageAsset)
            .OfType<CatalogImageAsset>()
            .DistinctBy(image => image.Id)
            .ToArray();

        product.DeletedAt = now;
        product.DeletedByAccountId = command.DeletedByAccountId;
        product.ImageAssetId = null;
        product.ImageAsset = null;

        foreach (var variant in product.ProductVariants)
        {
            variant.DeletedAt = now;
            variant.DeletedByAccountId = command.DeletedByAccountId;
            variant.ImageAssetId = null;
            variant.ImageAsset = null;
        }

        foreach (var image in imageAssetsToClean)
        {
            await _products.AddCatalogImageCleanupAsync(new CatalogImageCleanup
            {
                CatalogImageAssetId = image.Id,
                PublicIdSnapshot = image.PublicId
            }, cancellationToken);
        }

        await _products.SaveChangesAsync(cancellationToken);
        return ApiResult<bool>.Success(true, "Product deleted.");
    }
}
