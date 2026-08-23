using Application.Catalog.Abstractions;
using Application.Catalog.Images;
using Application.Catalog.Products.Mapping;
using Application.Catalog.Products.Results;
using Application.Shared.Wrappers;
using Domain.Catalog.Entities;
using Domain.Catalog.Enums;
using Microsoft.Extensions.Logging;
using System.Security.Cryptography;
using System.Text;

namespace Application.Catalog.Products.Commands;

public sealed class ReplaceCatalogImageCommandHandler
{
    private readonly IProductStore _products;
    private readonly ICatalogImageStorage _storage;
    private readonly ICatalogImageMutationCoordinator _mutations;
    private readonly ILogger<ReplaceCatalogImageCommandHandler> _logger;

    public ReplaceCatalogImageCommandHandler(
        IProductStore products,
        ICatalogImageStorage storage,
        ICatalogImageMutationCoordinator mutations,
        ILogger<ReplaceCatalogImageCommandHandler> logger)
    {
        _products = products;
        _storage = storage;
        _mutations = mutations;
        _logger = logger;
    }

    public Task<ApiResult<ProductResult>> ReplaceProductAsync(
        ProductManagementCommandScope scope, Guid productId, int expectedRevision, string? altText,
        byte[] content, string fileName, string contentType, string idempotencyKey, Guid? actorId, CancellationToken cancellationToken) =>
        _mutations.ExecuteAsync(
            productId,
            ct => ReplaceProductLockedAsync(scope, productId, expectedRevision, altText, content, fileName, contentType, idempotencyKey, actorId, ct),
            cancellationToken);

    private async Task<ApiResult<ProductResult>> ReplaceProductLockedAsync(
        ProductManagementCommandScope scope, Guid productId, int expectedRevision, string? altText,
        byte[] content, string fileName, string contentType, string idempotencyKey, Guid? actorId, CancellationToken cancellationToken)
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

        var normalizedAltText = NormalizeAltText(altText);
        if (normalizedAltText is null && !string.IsNullOrWhiteSpace(altText))
        {
            return ApiResult<ProductResult>.Fail("Image alt text must not exceed 500 characters.");
        }

        var normalizedKey = NormalizeIdempotencyKey(idempotencyKey);
        if (normalizedKey is null)
        {
            return ApiResult<ProductResult>.Fail("Idempotency-Key is required and must not exceed 200 characters.");
        }

        var fingerprint = CreateFingerprint(scope, "Product", product.Id, CatalogImageOperation.Replace, expectedRevision, normalizedAltText, content);
        var replay = await _products.GetCatalogImageOperationReplayAsync(
            ScopeKey(scope), "Product", product.Id, CatalogImageOperation.Replace, normalizedKey, cancellationToken);
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

        var previousImage = product.ImageAsset;
        var image = await ReplaceAsync(previousImage, product.OrganizationId, product.Id, null, content, fileName, contentType,
            uploadedImage =>
            {
                product.ImageAssetId = uploadedImage.Id;
                product.ImageAltText = normalizedAltText;
                product.UpdatedByAccountId = actorId;
            }, ScopeKey(scope), "Product", product.Id, normalizedKey, fingerprint, cancellationToken);

        product.ImageAsset = image;
        return ApiResult<ProductResult>.Success(ProductResultMapper.ToResult(product), "Product image updated.");
    }

    public Task<ApiResult<ProductVariantResult>> ReplaceVariantAsync(
        ProductManagementCommandScope scope, Guid productId, Guid variantId, int expectedRevision, string? altText,
        byte[] content, string fileName, string contentType, string idempotencyKey, Guid? actorId, CancellationToken cancellationToken) =>
        _mutations.ExecuteAsync(
            productId,
            ct => ReplaceVariantLockedAsync(scope, productId, variantId, expectedRevision, altText, content, fileName, contentType, idempotencyKey, actorId, ct),
            cancellationToken);

    private async Task<ApiResult<ProductVariantResult>> ReplaceVariantLockedAsync(
        ProductManagementCommandScope scope, Guid productId, Guid variantId, int expectedRevision, string? altText,
        byte[] content, string fileName, string contentType, string idempotencyKey, Guid? actorId, CancellationToken cancellationToken)
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

        var normalizedAltText = NormalizeAltText(altText);
        if (normalizedAltText is null && !string.IsNullOrWhiteSpace(altText))
        {
            return ApiResult<ProductVariantResult>.Fail("Image alt text must not exceed 500 characters.");
        }

        var normalizedKey = NormalizeIdempotencyKey(idempotencyKey);
        if (normalizedKey is null)
        {
            return ApiResult<ProductVariantResult>.Fail("Idempotency-Key is required and must not exceed 200 characters.");
        }

        var fingerprint = CreateFingerprint(scope, "ProductVariant", variant.Id, CatalogImageOperation.Replace, expectedRevision, normalizedAltText, content);
        var replay = await _products.GetCatalogImageOperationReplayAsync(
            ScopeKey(scope), "ProductVariant", variant.Id, CatalogImageOperation.Replace, normalizedKey, cancellationToken);
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

        var previousImage = variant.ImageAsset;
        var image = await ReplaceAsync(previousImage, product.OrganizationId, product.Id, variant.Id, content, fileName, contentType,
            uploadedImage =>
            {
                variant.ImageAssetId = uploadedImage.Id;
                variant.ImageAltText = normalizedAltText;
                variant.UpdatedByAccountId = actorId;
            }, ScopeKey(scope), "ProductVariant", variant.Id, normalizedKey, fingerprint, cancellationToken);

        variant.ImageAsset = image;
        return ApiResult<ProductVariantResult>.Success(ProductResultMapper.ToVariantResult(variant), "Product variant image updated.");
    }

    private async Task<CatalogImageAsset> ReplaceAsync(
        CatalogImageAsset? previousImage,
        Guid? organizationId,
        Guid productId,
        Guid? variantId,
        byte[] content,
        string fileName,
        string contentType,
        Action<CatalogImageAsset> assignImage,
        string scopeKey,
        string ownerType,
        Guid ownerId,
        string idempotencyKey,
        string requestFingerprint,
        CancellationToken cancellationToken)
    {
        var image = await UploadAsync(organizationId, productId, variantId, content, fileName, contentType, cancellationToken);
        try
        {
            assignImage(image);
            await _products.AddCatalogImageAssetAsync(image, cancellationToken);
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
                Operation = CatalogImageOperation.Replace,
                IdempotencyKey = idempotencyKey,
                RequestFingerprint = requestFingerprint
            }, cancellationToken);
            await _products.SaveChangesAsync(cancellationToken);
        }
        catch (Exception replacementException)
        {
            await CompensateFailedReplacementAsync(image, replacementException, cancellationToken);
            throw;
        }

        return image;
    }

    private async Task<CatalogImageAsset> UploadAsync(Guid? organizationId, Guid productId, Guid? variantId,
        byte[] content, string fileName, string contentType, CancellationToken cancellationToken)
    {
        var upload = await _storage.UploadAsync(new CatalogImageStorageUpload(
            content, fileName, contentType, BuildRelativePublicId(organizationId, productId, variantId)), cancellationToken);
        var image = new CatalogImageAsset
        {
            Provider = upload.Provider,
            ProviderAssetId = upload.ProviderAssetId,
            PublicId = upload.PublicId,
            DeliveryUrl = upload.DeliveryUrl,
            Version = upload.Version,
            Format = upload.Format,
            Width = upload.Width,
            Height = upload.Height,
            Bytes = upload.Bytes
        };
        return image;
    }

    private async Task CompensateFailedReplacementAsync(
        CatalogImageAsset image,
        Exception replacementException,
        CancellationToken cancellationToken)
    {
        try
        {
            await _storage.DeleteAsync(image.PublicId, cancellationToken);
        }
        catch (Exception cleanupException)
        {
            _logger.LogError(cleanupException,
                "Failed to delete Cloudinary catalog image {PublicId} after replacement persistence failed.", image.PublicId);
        }

        _logger.LogDebug(replacementException,
            "Catalog image replacement persistence failed after provider upload for {PublicId}.", image.PublicId);
    }

    private static string BuildRelativePublicId(Guid? organizationId, Guid productId, Guid? variantId)
    {
        var ownerPath = organizationId.HasValue
            ? variantId.HasValue
                ? $"organizations/{organizationId.Value}/products/{productId}/variants/{variantId.Value}"
                : $"organizations/{organizationId.Value}/products/{productId}"
            : variantId.HasValue
                ? $"platform/product-templates/{productId}/variants/{variantId.Value}"
                : $"platform/product-templates/{productId}";

        return $"{ownerPath}/{Guid.NewGuid():N}";
    }

    private static string? NormalizeIdempotencyKey(string? value) =>
        string.IsNullOrWhiteSpace(value) || value.Trim().Length > 200 ? null : value.Trim();

    private static string? NormalizeAltText(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        var normalized = value.Trim();
        return normalized.Length <= 500 ? normalized : null;
    }

    private static string ScopeKey(ProductManagementCommandScope scope) =>
        scope.IsGlobalTemplate ? "platform" : scope.OrganizationId?.ToString("D") ?? throw new InvalidOperationException("Organization scope is required.");

    private static string CreateFingerprint(
        ProductManagementCommandScope scope,
        string ownerType,
        Guid ownerId,
        CatalogImageOperation operation,
        int expectedRevision,
        string? altText,
        byte[] content)
    {
        var contentHash = Convert.ToHexString(SHA256.HashData(content));
        var normalizedAltText = string.IsNullOrWhiteSpace(altText) ? string.Empty : altText.Trim();
        var material = string.Join('|', ScopeKey(scope), ownerType, ownerId.ToString("D"), operation, expectedRevision, normalizedAltText, contentHash);
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(material)));
    }
}
