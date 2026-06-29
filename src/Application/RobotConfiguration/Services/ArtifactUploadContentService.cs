using System.Security.Cryptography;
using System.Text.Json;
using Application.RobotConfiguration.Abstractions;
using Microsoft.Extensions.Logging;

namespace Application.RobotConfiguration.Services;

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
        var content = new MemoryStream((int)expectedLength);
        using var sha256 = SHA256.Create();
        var buffer = new byte[BufferSize];
        int read;
        while ((read = await source.ReadAsync(buffer, cancellationToken)) > 0)
        {
            await content.WriteAsync(buffer.AsMemory(0, read), cancellationToken);
            sha256.TransformBlock(buffer, 0, read, null, 0);
        }

        sha256.TransformFinalBlock([], 0, 0);
        content.Position = 0;
        return new BufferedArtifactContent(
            content,
            Convert.ToHexString(sha256.Hash!).ToLowerInvariant());
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
        try
        {
            await _storage.DeleteIfExistsAsync(storageKey, CancellationToken.None);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                ex,
                "Failed to compensate uncommitted robot artifact object {StorageKey}; orphan cleanup will retry.",
                storageKey);
        }
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
