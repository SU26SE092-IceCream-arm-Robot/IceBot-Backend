using Application.Catalog.Abstractions;
using Application.Catalog.Products.Mapping;
using Application.Catalog.Products.Results;
using Application.Shared.Concurrency;
using Application.Shared.Wrappers;
using Domain.Catalog.Entities;
using Domain.Catalog.Enums;
using System.Security.Cryptography;
using System.Text;

namespace Application.Catalog.Products.Commands;

public sealed class RemoveCatalogImageCommandHandler
{
    private readonly IProductStore _products;
    private readonly ITechnicalResourceMutationCoordinator _mutations;

    public RemoveCatalogImageCommandHandler(
        IProductStore products,
        ITechnicalResourceMutationCoordinator mutations)
    {
        _products = products;
        _mutations = mutations;
    }

    public Task<ApiResult<ProductResult>> RemoveProductAsync(
        ProductManagementCommandScope scope,
        Guid productId,
        int expectedRevision,
        string idempotencyKey,
        Guid? actorId,
        CancellationToken cancellationToken) =>
        _mutations.ExecuteAsync(
            [TechnicalResourceMutationIdentity.Product(productId)],
            ct => RemoveProductLockedAsync(scope, productId, expectedRevision, idempotencyKey, actorId, ct),
            cancellationToken);

    public Task<ApiResult<ProductVariantResult>> RemoveVariantAsync(
        ProductManagementCommandScope scope,
        Guid productId,
        Guid variantId,
        int expectedRevision,
        string idempotencyKey,
        Guid? actorId,
        CancellationToken cancellationToken) =>
        _mutations.ExecuteAsync(
            [TechnicalResourceMutationIdentity.Product(productId)],
            ct => RemoveVariantLockedAsync(scope, productId, variantId, expectedRevision, idempotencyKey, actorId, ct),
            cancellationToken);

    private async Task<ApiResult<ProductResult>> RemoveProductLockedAsync(
        ProductManagementCommandScope scope,
        Guid productId,
        int expectedRevision,
        string idempotencyKey,
        Guid? actorId,
        CancellationToken cancellationToken)
    {
        var product = await _products.GetProductByIdAsync(productId, asNoTracking: false, cancellationToken: cancellationToken);
        if (product is null)
        {
            return ApiResult<ProductResult>.Fail("Product not found.", 404);
        }

        var accessError = ProductManagementCommandRules.ValidateExisting<ProductResult>(scope, product);
        if (accessError is not null)
        {
            return accessError;
        }

        var key = NormalizeIdempotencyKey(idempotencyKey);
        if (key is null)
        {
            return ApiResult<ProductResult>.Fail("Idempotency-Key is required and must not exceed 200 characters.");
        }

        var fingerprint = CreateFingerprint(scope, "Product", product.Id, expectedRevision);
        var replay = await _products.GetCatalogImageOperationReplayAsync(
            ScopeKey(scope), "Product", product.Id, CatalogImageOperation.Remove, key, cancellationToken);
        if (replay is not null)
        {
            return replay.RequestFingerprint == fingerprint
                ? ApiResult<ProductResult>.Success(ProductResultMapper.ToResult(product), "Product image operation already completed.")
                : ApiResult<ProductResult>.Fail("Idempotency-Key was already used with a different request.", 409);
        }

        if (product.Revision != expectedRevision)
        {
            return ApiResult<ProductResult>.Fail("Product was changed by another operation.", 409);
        }

        await RemoveAsync(product.ImageAsset, () =>
        {
            product.ImageAssetId = null;
            product.ImageAsset = null;
            product.ImageAltText = null;
            product.UpdatedAt = DateTimeOffset.UtcNow;
            product.UpdatedByAccountId = actorId;
        }, ScopeKey(scope), "Product", product.Id, key, fingerprint, cancellationToken);

        return ApiResult<ProductResult>.Success(ProductResultMapper.ToResult(product), "Product image removed.");
    }

    private async Task<ApiResult<ProductVariantResult>> RemoveVariantLockedAsync(
        ProductManagementCommandScope scope,
        Guid productId,
        Guid variantId,
        int expectedRevision,
        string idempotencyKey,
        Guid? actorId,
        CancellationToken cancellationToken)
    {
        var product = await _products.GetProductByIdAsync(productId, cancellationToken: cancellationToken);
        if (product is null)
        {
            return ApiResult<ProductVariantResult>.Fail("Product not found.", 404);
        }

        var accessError = ProductManagementCommandRules.ValidateExisting<ProductVariantResult>(scope, product);
        if (accessError is not null)
        {
            return accessError;
        }

        var variant = await _products.GetProductVariantByIdAsync(productId, variantId, asNoTracking: false, cancellationToken: cancellationToken);
        if (variant is null)
        {
            return ApiResult<ProductVariantResult>.Fail("Product variant not found.", 404);
        }

        var key = NormalizeIdempotencyKey(idempotencyKey);
        if (key is null)
        {
            return ApiResult<ProductVariantResult>.Fail("Idempotency-Key is required and must not exceed 200 characters.");
        }

        var fingerprint = CreateFingerprint(scope, "ProductVariant", variant.Id, expectedRevision);
        var replay = await _products.GetCatalogImageOperationReplayAsync(
            ScopeKey(scope), "ProductVariant", variant.Id, CatalogImageOperation.Remove, key, cancellationToken);
        if (replay is not null)
        {
            return replay.RequestFingerprint == fingerprint
                ? ApiResult<ProductVariantResult>.Success(ProductResultMapper.ToVariantResult(variant), "Product variant image operation already completed.")
                : ApiResult<ProductVariantResult>.Fail("Idempotency-Key was already used with a different request.", 409);
        }

        if (variant.Revision != expectedRevision)
        {
            return ApiResult<ProductVariantResult>.Fail("Product variant was changed by another operation.", 409);
        }

        await RemoveAsync(variant.ImageAsset, () =>
        {
            variant.ImageAssetId = null;
            variant.ImageAsset = null;
            variant.ImageAltText = null;
            variant.UpdatedAt = DateTimeOffset.UtcNow;
            variant.UpdatedByAccountId = actorId;
        }, ScopeKey(scope), "ProductVariant", variant.Id, key, fingerprint, cancellationToken);

        return ApiResult<ProductVariantResult>.Success(ProductResultMapper.ToVariantResult(variant), "Product variant image removed.");
    }

    private async Task RemoveAsync(
        CatalogImageAsset? previousImage,
        Action clearOwner,
        string scopeKey,
        string ownerType,
        Guid ownerId,
        string idempotencyKey,
        string fingerprint,
        CancellationToken cancellationToken)
    {
        clearOwner();
        if (previousImage is not null)
        {
            await _products.AddCatalogImageCleanupAsync(new CatalogImageCleanup
            {
                CatalogImageAssetId = previousImage.Id,
                PublicIdSnapshot = previousImage.PublicId
            }, cancellationToken);
        }

        await _products.AddCatalogImageOperationReplayAsync(new CatalogImageOperationReplay
        {
            ScopeKey = scopeKey,
            OwnerType = ownerType,
            OwnerId = ownerId,
            Operation = CatalogImageOperation.Remove,
            IdempotencyKey = idempotencyKey,
            RequestFingerprint = fingerprint
        }, cancellationToken);
        await _products.SaveChangesAsync(cancellationToken);
    }

    private static string? NormalizeIdempotencyKey(string? value) =>
        string.IsNullOrWhiteSpace(value) || value.Trim().Length > 200 ? null : value.Trim();

    private static string ScopeKey(ProductManagementCommandScope scope) =>
        scope.IsGlobalTemplate ? "platform" : scope.OrganizationId?.ToString("D") ?? throw new InvalidOperationException("Organization scope is required.");

    private static string CreateFingerprint(ProductManagementCommandScope scope, string ownerType, Guid ownerId, int expectedRevision)
    {
        var material = string.Join('|', ScopeKey(scope), ownerType, ownerId.ToString("D"), CatalogImageOperation.Remove, expectedRevision);
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(material)));
    }
}
