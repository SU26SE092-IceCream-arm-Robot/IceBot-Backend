using Application.Catalog.Images;
using Application.Shared.Exceptions;
using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Microsoft.Extensions.Options;

namespace Infrastructure.Catalog.Images;

public sealed class CloudinaryCatalogImageStorage : ICatalogImageStorage, ICatalogImageStorageHealthProbe
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
        EnsureConfigured();

        if (upload.Content.Length == 0 || upload.Content.Length > _options.MaxUploadBytes)
        {
            throw new CatalogImageUploadValidationException($"Catalog image must be between 1 and {_options.MaxUploadBytes} bytes.");
        }

        if (!AllowedContentTypes.TryGetValue(upload.ContentType, out var extension) || !HasExpectedSignature(upload.Content, extension))
        {
            throw new CatalogImageUploadValidationException("Catalog image must be a JPEG, PNG, or WebP file with a matching content signature.");
        }

        if (!TryReadDimensions(upload.Content, extension, out var width, out var height) ||
            width < _options.MinDimensionPixels || height < _options.MinDimensionPixels ||
            width > _options.MaxDimensionPixels || height > _options.MaxDimensionPixels)
        {
            throw new CatalogImageUploadValidationException(
                $"Catalog image dimensions must be between {_options.MinDimensionPixels} and {_options.MaxDimensionPixels} pixels.");
        }

        var rootFolder = NormalizeRootFolder(_options.RootFolder);
        var relativePublicId = NormalizeRelativePublicId(upload.PublicId, rootFolder);

        try
        {
            var cloudinary = CreateClient();
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
        catch (AppException)
        {
            throw;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            throw CreateUnavailableException(exception);
        }
    }

    public async Task DeleteAsync(string publicId, CancellationToken cancellationToken = default)
    {
        EnsureConfigured();
        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            var result = await CreateClient().DestroyAsync(new DeletionParams(publicId)
            {
                ResourceType = ResourceType.Image,
                Invalidate = true
            });
            if (result.Error is not null ||
                (!string.IsNullOrWhiteSpace(result.Result) &&
                 !string.Equals(result.Result, "ok", StringComparison.OrdinalIgnoreCase) &&
                 !string.Equals(result.Result, "not found", StringComparison.OrdinalIgnoreCase)))
            {
                throw new AppException("Catalog image storage is unavailable.", 503);
            }
        }
        catch (AppException)
        {
            throw;
        }
        catch (Exception exception)
        {
            throw CreateUnavailableException(exception);
        }
    }

    public async Task EnsureReadyAsync(CancellationToken cancellationToken = default)
    {
        EnsureConfigured();

        try
        {
            var result = await CreateClient().PingAsync(cancellationToken);
            if (result.Error is not null)
            {
                throw new AppException("Catalog image storage is unavailable.", 503);
            }
        }
        catch (AppException)
        {
            throw;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            throw CreateUnavailableException(exception);
        }
    }

    private Cloudinary CreateClient() => new(new Account(_options.CloudName, _options.ApiKey, _options.ApiSecret));

    private void EnsureConfigured()
    {
        if (string.IsNullOrWhiteSpace(_options.CloudName) ||
            string.IsNullOrWhiteSpace(_options.ApiKey) ||
            string.IsNullOrWhiteSpace(_options.ApiSecret) ||
            string.IsNullOrWhiteSpace(_options.RootFolder) ||
            _options.MaxUploadBytes is < 1 or > 20 * 1024 * 1024)
        {
            throw new AppException("Catalog image storage is not configured.", 503);
        }

        if (_options.MinDimensionPixels is < 1 or > 4096 ||
            _options.MaxDimensionPixels is < 400 or > 4096 ||
            _options.MinDimensionPixels > _options.MaxDimensionPixels)
        {
            throw new AppException("Catalog image storage is not configured.", 503);
        }

        try
        {
            _ = NormalizeRootFolder(_options.RootFolder);
        }
        catch (InvalidOperationException)
        {
            throw new AppException("Catalog image storage is not configured.", 503);
        }
    }

    private static AppException CreateUnavailableException(Exception exception)
    {
        _ = exception;
        return new AppException("Catalog image storage is unavailable.", 503);
    }

    private static bool HasExpectedSignature(byte[] content, string extension) => extension switch
    {
        "jpg" => content.Length >= 3 && content[0] == 0xFF && content[1] == 0xD8 && content[2] == 0xFF,
        "png" => content.Length >= 8 && content.AsSpan(0, 8).SequenceEqual(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 }),
        "webp" => content.Length >= 12 && content.AsSpan(0, 4).SequenceEqual("RIFF"u8) && content.AsSpan(8, 4).SequenceEqual("WEBP"u8),
        _ => false
    };

    private static bool TryReadDimensions(byte[] content, string extension, out int width, out int height)
    {
        return extension switch
        {
            "png" => TryReadPngDimensions(content, out width, out height),
            "jpg" => TryReadJpegDimensions(content, out width, out height),
            "webp" => TryReadWebpDimensions(content, out width, out height),
            _ => FailDimensions(out width, out height)
        };
    }

    private static bool TryReadPngDimensions(byte[] content, out int width, out int height)
    {
        if (content.Length < 24)
            return FailDimensions(out width, out height);

        width = ReadInt32BigEndian(content, 16);
        height = ReadInt32BigEndian(content, 20);
        return width > 0 && height > 0;
    }

    private static bool TryReadJpegDimensions(byte[] content, out int width, out int height)
    {
        for (var index = 2; index + 8 < content.Length;)
        {
            while (index < content.Length && content[index] != 0xFF)
                index++;
            while (index < content.Length && content[index] == 0xFF)
                index++;
            if (index >= content.Length)
                break;

            var marker = content[index++];
            if (marker is 0xD8 or 0xD9 || marker is >= 0xD0 and <= 0xD7 || marker == 0x01)
                continue;
            if (index + 1 >= content.Length)
                break;

            var segmentLength = (content[index] << 8) | content[index + 1];
            if (segmentLength < 2 || index + segmentLength > content.Length)
                break;

            if (segmentLength >= 8 && (marker is >= 0xC0 and <= 0xC3 or >= 0xC5 and <= 0xC7 or >= 0xC9 and <= 0xCB or >= 0xCD and <= 0xCF))
            {
                height = (content[index + 3] << 8) | content[index + 4];
                width = (content[index + 5] << 8) | content[index + 6];
                return width > 0 && height > 0;
            }

            index += segmentLength;
        }

        return FailDimensions(out width, out height);
    }

    private static bool TryReadWebpDimensions(byte[] content, out int width, out int height)
    {
        if (content.Length < 30)
            return FailDimensions(out width, out height);

        var chunkType = System.Text.Encoding.ASCII.GetString(content, 12, 4);
        switch (chunkType)
        {
            case "VP8X" when content.Length >= 30:
                width = 1 + ReadInt24LittleEndian(content, 24);
                height = 1 + ReadInt24LittleEndian(content, 27);
                return width > 0 && height > 0;
            case "VP8 " when content.Length >= 30 && content[23] == 0x9D && content[24] == 0x01 && content[25] == 0x2A:
                width = ((content[27] & 0x3F) << 8) | content[26];
                height = ((content[29] & 0x3F) << 8) | content[28];
                return width > 0 && height > 0;
            case "VP8L" when content.Length >= 25 && content[20] == 0x2F:
                width = 1 + content[21] + ((content[22] & 0x3F) << 8);
                height = 1 + ((content[22] & 0xC0) >> 6) + (content[23] << 2) + ((content[24] & 0x0F) << 10);
                return width > 0 && height > 0;
            default:
                return FailDimensions(out width, out height);
        }
    }

    private static int ReadInt32BigEndian(byte[] content, int offset) =>
        (content[offset] << 24) | (content[offset + 1] << 16) | (content[offset + 2] << 8) | content[offset + 3];

    private static int ReadInt24LittleEndian(byte[] content, int offset) =>
        content[offset] | (content[offset + 1] << 8) | (content[offset + 2] << 16);

    private static bool FailDimensions(out int width, out int height)
    {
        width = 0;
        height = 0;
        return false;
    }

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
