using Application.Catalog.Images;
using Application.Shared.Exceptions;
using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Microsoft.Extensions.Options;

namespace Infrastructure.Catalog.Images;

public sealed class CloudinaryCatalogImageStorage : ICatalogImageStorage
{
    private static readonly IReadOnlyDictionary<string, string> AllowedContentTypes = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        ["image/jpeg"] = "jpg",
        ["image/png"] = "png",
        ["image/webp"] = "webp"
    };

    private readonly CloudinaryCatalogImageStorageOptions _options;

    public CloudinaryCatalogImageStorage(IOptions<CloudinaryCatalogImageStorageOptions> options)
    {
        _options = options.Value;
    }

    public async Task<CatalogImageStorageResult> UploadAsync(CatalogImageStorageUpload upload, CancellationToken cancellationToken = default)
    {
        if (upload.Content.Length == 0 || upload.Content.Length > _options.MaxUploadBytes)
        {
            throw new CatalogImageUploadValidationException($"Catalog image must be between 1 and {_options.MaxUploadBytes} bytes.");
        }

        if (!AllowedContentTypes.TryGetValue(upload.ContentType, out var extension) || !HasExpectedSignature(upload.Content, extension))
        {
            throw new CatalogImageUploadValidationException("Catalog image must be a JPEG, PNG, or WebP file with a matching content signature.");
        }

        var rootFolder = NormalizeRootFolder(_options.RootFolder);
        var relativePublicId = NormalizeRelativePublicId(upload.PublicId, rootFolder);
        var account = new Account(_options.CloudName, _options.ApiKey, _options.ApiSecret);
        var cloudinary = new Cloudinary(account);
        await using var content = new MemoryStream(upload.Content, writable: false);
        var result = await cloudinary.UploadAsync(new ImageUploadParams
        {
            File = new FileDescription(upload.FileName, content),
            PublicId = $"{rootFolder}/{relativePublicId}",
            Overwrite = false,
            UniqueFilename = false,
            UseFilename = false
        }, cancellationToken);

        if (result.Error is not null || string.IsNullOrWhiteSpace(result.AssetId) || result.SecureUrl is null)
        {
            throw new AppException("Catalog image storage is unavailable.", 503);
        }

        return new CatalogImageStorageResult(
            "Cloudinary", result.AssetId, result.PublicId, result.SecureUrl.AbsoluteUri,
            int.TryParse(result.Version, out var version) ? version : 0,
            result.Format, result.Width, result.Height, result.Bytes);
    }

    public async Task DeleteAsync(string publicId, CancellationToken cancellationToken = default)
    {
        var cloudinary = new Cloudinary(new Account(_options.CloudName, _options.ApiKey, _options.ApiSecret));
        var result = await cloudinary.DestroyAsync(new DeletionParams(publicId)
        {
            ResourceType = ResourceType.Image
        });
        if (result.Error is not null)
        {
            throw new InvalidOperationException($"Cloudinary image deletion failed: {result.Error.Message}.");
        }
    }

    private static bool HasExpectedSignature(byte[] content, string extension) => extension switch
    {
        "jpg" => content.Length >= 3 && content[0] == 0xFF && content[1] == 0xD8 && content[2] == 0xFF,
        "png" => content.Length >= 8 && content.AsSpan(0, 8).SequenceEqual(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 }),
        "webp" => content.Length >= 12 && content.AsSpan(0, 4).SequenceEqual("RIFF"u8) && content.AsSpan(8, 4).SequenceEqual("WEBP"u8),
        _ => false
    };

    private static string NormalizeRootFolder(string rootFolder)
    {
        var normalized = NormalizePath(rootFolder, "Cloudinary root folder");
        return normalized;
    }

    private static string NormalizeRelativePublicId(string publicId, string rootFolder)
    {
        if (publicId.StartsWith("/", StringComparison.Ordinal))
        {
            throw new InvalidOperationException("Catalog image public IDs must be relative to the configured Cloudinary root folder.");
        }

        var normalized = NormalizePath(publicId, "Catalog image public ID");
        if (normalized.Equals(rootFolder, StringComparison.OrdinalIgnoreCase) ||
            normalized.StartsWith($"{rootFolder}/", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Catalog image public IDs must not include the configured Cloudinary root folder.");
        }

        return normalized;
    }

    private static string NormalizePath(string value, string label)
    {
        var normalized = value.Trim().Trim('/');
        if (string.IsNullOrWhiteSpace(normalized) ||
            normalized.Split('/', StringSplitOptions.None).Any(segment => string.IsNullOrWhiteSpace(segment) || segment is "." or ".."))
        {
            throw new InvalidOperationException($"{label} must contain non-empty path segments and cannot contain '.' or '..'.");
        }

        return normalized;
    }
}
