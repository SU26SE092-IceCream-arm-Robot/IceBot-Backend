using Application.RobotConfiguration.Abstractions;
using Microsoft.Extensions.Options;
using Minio;
using Minio.DataModel.Args;
using Minio.Exceptions;

namespace Infrastructure.RobotConfiguration.ObjectStorage;

public sealed class MinioArtifactObjectStorage : IArtifactObjectStorage
{
    private readonly RobotArtifactObjectStorageOptions _options;
    private readonly IMinioClient _client;
    private readonly IMinioClient _downloadClient;

    public MinioArtifactObjectStorage(IOptions<RobotArtifactObjectStorageOptions> options)
    {
        _options = options.Value;
        if (string.IsNullOrWhiteSpace(_options.Endpoint) ||
            string.IsNullOrWhiteSpace(_options.AccessKey) ||
            string.IsNullOrWhiteSpace(_options.SecretKey) ||
            string.IsNullOrWhiteSpace(_options.BucketName))
        {
            throw new InvalidOperationException("Robot artifact object storage is not configured.");
        }

        _client = BuildClient(_options.Endpoint, _options.UseSsl);
        _downloadClient = string.IsNullOrWhiteSpace(_options.DownloadEndpoint)
            ? _client
            : BuildClient(_options.DownloadEndpoint, _options.DownloadUseSsl ?? _options.UseSsl);
    }

    public async Task<bool> ExistsAsync(string storageKey, CancellationToken cancellationToken = default)
    {
        try
        {
            await _client.StatObjectAsync(
                new StatObjectArgs()
                    .WithBucket(_options.BucketName)
                    .WithObject(storageKey),
                cancellationToken);
            return true;
        }
        catch (ObjectNotFoundException)
        {
            return false;
        }
        catch (BucketNotFoundException)
        {
            return false;
        }
    }

    public async Task<ArtifactObjectWriteResult> WriteImmutableAsync(
        ArtifactObjectWriteRequest request,
        Stream content,
        CancellationToken cancellationToken = default)
    {
        await EnsureReadyAsync(cancellationToken);

        if (await ExistsAsync(request.StorageKey, cancellationToken))
        {
            throw new ArtifactObjectAlreadyExistsException(request.StorageKey);
        }

        await _client.PutObjectAsync(
            new PutObjectArgs()
                .WithBucket(_options.BucketName)
                .WithObject(request.StorageKey)
                .WithStreamData(content)
                .WithObjectSize(request.ContentLengthBytes)
                .WithContentType(request.ContentType),
            cancellationToken);

        return new ArtifactObjectWriteResult(request.StorageKey, request.Checksum, request.ContentLengthBytes);
    }

    public async Task<ArtifactObjectReadUrlResult> CreateReadUrlAsync(
        string storageKey,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (string.IsNullOrWhiteSpace(storageKey))
        {
            throw new ArgumentException("Artifact storage key is required.", nameof(storageKey));
        }

        var expirySeconds = Math.Clamp(_options.DownloadUrlExpirySeconds, 60, 604800);
        var expiresAt = DateTimeOffset.UtcNow.AddSeconds(expirySeconds);
        var url = await _downloadClient.PresignedGetObjectAsync(
            new PresignedGetObjectArgs()
                .WithBucket(_options.BucketName)
                .WithObject(storageKey)
                .WithExpiry(expirySeconds));

        return new ArtifactObjectReadUrlResult(url, expiresAt);
    }

    public async Task<ArtifactObjectWriteResult> CopyImmutableAsync(
        string sourceStorageKey,
        ArtifactObjectWriteRequest destination,
        CancellationToken cancellationToken = default)
    {
        await EnsureReadyAsync(cancellationToken);
        if (!await ExistsAsync(sourceStorageKey, cancellationToken))
            throw new InvalidOperationException("Source artifact object does not exist.");
        if (await ExistsAsync(destination.StorageKey, cancellationToken))
            throw new ArtifactObjectAlreadyExistsException(destination.StorageKey);

        await _client.CopyObjectAsync(
            new CopyObjectArgs()
                .WithBucket(_options.BucketName)
                .WithObject(destination.StorageKey)
                .WithCopyObjectSource(new CopySourceObjectArgs()
                    .WithBucket(_options.BucketName)
                    .WithObject(sourceStorageKey)),
            cancellationToken);

        return new ArtifactObjectWriteResult(
            destination.StorageKey,
            destination.Checksum,
            destination.ContentLengthBytes);
    }

    public async IAsyncEnumerable<ArtifactObjectInfo> ListAsync(
        string prefix,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await EnsureReadyAsync(cancellationToken);

        var args = new ListObjectsArgs()
            .WithBucket(_options.BucketName)
            .WithPrefix(prefix)
            .WithRecursive(true);

        await foreach (var item in _client.ListObjectsEnumAsync(args, cancellationToken))
        {
            if (!item.LastModifiedDateTime.HasValue)
            {
                continue;
            }

            yield return new ArtifactObjectInfo(
                item.Key,
                new DateTimeOffset(item.LastModifiedDateTime.Value.ToUniversalTime()),
                (long)item.Size);
        }
    }

    public async Task DeleteIfExistsAsync(string storageKey, CancellationToken cancellationToken = default)
    {
        if (!await ExistsAsync(storageKey, cancellationToken))
        {
            return;
        }

        await _client.RemoveObjectAsync(
            new RemoveObjectArgs()
                .WithBucket(_options.BucketName)
                .WithObject(storageKey),
            cancellationToken);
    }

    private IMinioClient BuildClient(string endpoint, bool useSsl)
    {
        return new MinioClient()
            .WithEndpoint(endpoint)
            .WithCredentials(_options.AccessKey, _options.SecretKey)
            .WithSSL(useSsl)
            .Build();
    }

    public async Task EnsureReadyAsync(CancellationToken cancellationToken = default)
    {
        var exists = await _client.BucketExistsAsync(
            new BucketExistsArgs().WithBucket(_options.BucketName),
            cancellationToken);

        if (exists)
        {
            return;
        }

        if (!_options.AutoCreateBucket)
        {
            throw new InvalidOperationException(
                $"Robot artifact bucket '{_options.BucketName}' does not exist and automatic creation is disabled.");
        }

        try
        {
            await _client.MakeBucketAsync(
                new MakeBucketArgs().WithBucket(_options.BucketName),
                cancellationToken);
        }
        catch (MinioException)
        {
            // Another backend instance may have created the bucket after the existence check.
            if (!await _client.BucketExistsAsync(
                    new BucketExistsArgs().WithBucket(_options.BucketName),
                    cancellationToken))
            {
                throw;
            }
        }
    }
}
