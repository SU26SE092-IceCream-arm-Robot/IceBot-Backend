using Application.Catalog.Abstractions;
using Application.Catalog.Images;
using Application.Catalog.Products.Mapping;
using Application.Catalog.Products.Results;
using Application.Shared.Wrappers;
using Domain.Catalog.Entities;
using Microsoft.Extensions.Logging;

namespace Application.Catalog.Products.Commands;

public sealed class ReplaceCatalogImageCommandHandler
{
    private readonly IProductStore _products;
    private readonly ICatalogImageStorage _storage;
    private readonly ILogger<ReplaceCatalogImageCommandHandler> _logger;

    public ReplaceCatalogImageCommandHandler(
        IProductStore products,
        ICatalogImageStorage storage,
        ILogger<ReplaceCatalogImageCommandHandler> logger)
    {
        _products = products;
        _storage = storage;
        _logger = logger;
    }

    public async Task<ApiResult<ProductResult>> ReplaceProductAsync(
        ProductManagementCommandScope scope, Guid productId, int expectedRevision, string? altText,
        byte[] content, string fileName, string contentType, Guid? actorId, CancellationToken cancellationToken)
    {
        var product = await _products.GetProductByIdAsync(productId, asNoTracking: false, cancellationToken: cancellationToken);
        if (product is null)
        {
            return ApiResult<ProductResult>.Fail("Product not found.", 404);
        }

        var accessError = ProductManagementCommandRules.ValidateExisting<ProductResult>(scope, product);
        if (accessError is not null || product.Revision != expectedRevision)
        {
            return accessError ?? ApiResult<ProductResult>.Fail("Product was changed by another operation.", 409);
        }

        var previousImage = product.ImageAsset;
        var image = await ReplaceAsync(previousImage, product.OrganizationId, product.Id, null, content, fileName, contentType,
            uploadedImage =>
            {
                product.ImageAssetId = uploadedImage.Id;
                product.ImageAltText = string.IsNullOrWhiteSpace(altText) ? null : altText.Trim();
                product.UpdatedByAccountId = actorId;
            }, cancellationToken);

        product.ImageAsset = image;
        return ApiResult<ProductResult>.Success(ProductResultMapper.ToResult(product), "Product image updated.");
    }

    public async Task<ApiResult<ProductVariantResult>> ReplaceVariantAsync(
        ProductManagementCommandScope scope, Guid productId, Guid variantId, int expectedRevision, string? altText,
        byte[] content, string fileName, string contentType, Guid? actorId, CancellationToken cancellationToken)
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

        if (variant.Revision != expectedRevision)
        {
            return ApiResult<ProductVariantResult>.Fail("Product variant was changed by another operation.", 409);
        }

        var previousImage = variant.ImageAsset;
        var image = await ReplaceAsync(previousImage, product.OrganizationId, product.Id, variant.Id, content, fileName, contentType,
            uploadedImage =>
            {
                variant.ImageAssetId = uploadedImage.Id;
                variant.ImageAltText = string.IsNullOrWhiteSpace(altText) ? null : altText.Trim();
                variant.UpdatedByAccountId = actorId;
            }, cancellationToken);

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
        CancellationToken cancellationToken)
    {
        var image = await UploadAsync(organizationId, productId, variantId, content, fileName, contentType, cancellationToken);
        try
        {
            assignImage(image);
            await _products.AddCatalogImageAssetAsync(image, cancellationToken);
            await _products.SaveChangesAsync(cancellationToken);
        }
        catch (Exception replacementException)
        {
            await CompensateFailedReplacementAsync(image, replacementException, cancellationToken);
            throw;
        }

        await RetirePreviousImageAsync(previousImage, cancellationToken);
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

    private async Task RetirePreviousImageAsync(CatalogImageAsset? previousImage, CancellationToken cancellationToken)
    {
        if (previousImage is null ||
            await _products.IsCatalogImageAssetReferencedAsync(previousImage.Id, cancellationToken))
        {
            return;
        }

        try
        {
            await _storage.DeleteAsync(previousImage.PublicId, cancellationToken);
            previousImage.Status = Domain.Catalog.Enums.CatalogImageAssetStatus.Deleted;
            await _products.SaveChangesAsync(cancellationToken);
        }
        catch (Exception exception)
        {
            _logger.LogError(exception,
                "Failed to retire unreferenced Cloudinary catalog image {PublicId}.", previousImage.PublicId);
        }
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
}
