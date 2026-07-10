using System.Security.Cryptography;
using System.Text.Json;
using Application.RobotConfiguration.Storage.Abstractions;
using Microsoft.Extensions.Logging;

namespace Application.RobotConfiguration.Storage.Services;

public sealed class ArtifactUploadContentService
{
    private const int BufferSize = 81920;
    public const long MaximumFileSizeBytes = 10 * 1024 * 1024;

    private readonly IArtifactObjectStorage _storage;
    private readonly ILogger<ArtifactUploadContentService> _logger;

    public ArtifactUploadContentService(
        IArtifactObjectStorage storage,
        ILogger<ArtifactUploadContentService> logger)
    {
        _storage = storage;
        _logger = logger;
    }

    public static bool IsValidMetadataJson(string? metadataJson)
    {
        if (string.IsNullOrWhiteSpace(metadataJson))
        {
            return true;
        }

        try
        {
            using var _ = JsonDocument.Parse(metadataJson);
            return true;
        }
        catch (JsonException)
        {
            return false;
        }
    }

    public async Task<BufferedArtifactContent> BufferAndHashAsync(
        Stream source,
        long expectedLength,
        CancellationToken cancellationToken)
    {
        if (expectedLength <= 0 || expectedLength > MaximumFileSizeBytes)
        {
            throw new ArtifactUploadContentException(
                $"Artifact content length must be between 1 byte and {MaximumFileSizeBytes} bytes.");
        }

        var content = new MemoryStream((int)expectedLength);
        try
        {
            using var sha256 = SHA256.Create();
            var buffer = new byte[BufferSize];
            long totalBytes = 0;
            int read;
            while ((read = await source.ReadAsync(buffer, cancellationToken)) > 0)
            {
                totalBytes += read;
                if (totalBytes > expectedLength || totalBytes > MaximumFileSizeBytes)
                {
                    throw new ArtifactUploadContentException(
                        "Uploaded content exceeds the declared or maximum artifact size.");
                }

                await content.WriteAsync(buffer.AsMemory(0, read), cancellationToken);
                sha256.TransformBlock(buffer, 0, read, null, 0);
            }

            if (totalBytes != expectedLength)
            {
                throw new ArtifactUploadContentException(
                    "Uploaded content length does not match the declared content length.");
            }

            sha256.TransformFinalBlock([], 0, 0);
            content.Position = 0;
            return new BufferedArtifactContent(
                content,
                Convert.ToHexString(sha256.Hash!).ToLowerInvariant());
        }
        catch
        {
            await content.DisposeAsync();
            throw;
        }
    }

    public Task<ArtifactObjectWriteResult> WriteImmutableAsync(
        string storageKey,
        string? contentType,
        BufferedArtifactContent content,
        CancellationToken cancellationToken) =>
        _storage.WriteImmutableAsync(
            new ArtifactObjectWriteRequest(
                storageKey,
                string.IsNullOrWhiteSpace(contentType) ? "application/octet-stream" : contentType.Trim(),
                content.Stream.Length,
                content.Checksum),
            content.Stream,
            cancellationToken);

    public async Task DeleteUncommittedObjectAsync(string storageKey)
    {
        await TryDeleteObjectAsync(storageKey, CancellationToken.None);
    }

    public async Task<bool> TryDeleteObjectAsync(
        string storageKey,
        CancellationToken cancellationToken)
    {
        try
        {
            await _storage.DeleteIfExistsAsync(storageKey, cancellationToken);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                ex,
                "Failed to compensate uncommitted robot artifact object {StorageKey}; orphan cleanup will retry.",
                storageKey);
            return false;
        }
    }
}

public sealed class ArtifactUploadContentException : Exception
{
    public ArtifactUploadContentException(string message) : base(message)
    {
    }
}

public sealed class BufferedArtifactContent : IAsyncDisposable
{
    public BufferedArtifactContent(MemoryStream stream, string checksum)
    {
        Stream = stream;
        Checksum = checksum;
    }

    public MemoryStream Stream { get; }
    public string Checksum { get; }

    public ValueTask DisposeAsync() => Stream.DisposeAsync();
}
