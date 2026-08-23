namespace Application.Catalog.Images;

public interface ICatalogImageStorage
{
    Task<CatalogImageStorageResult> UploadAsync(CatalogImageStorageUpload upload, CancellationToken cancellationToken = default);

    Task DeleteAsync(string publicId, CancellationToken cancellationToken = default);
}

public sealed record CatalogImageStorageUpload(
    byte[] Content,
    string FileName,
    string ContentType,
    string PublicId);

public sealed record CatalogImageStorageResult(
    string Provider,
    string ProviderAssetId,
    string PublicId,
    string DeliveryUrl,
    int Version,
    string Format,
    int Width,
    int Height,
    long Bytes);
