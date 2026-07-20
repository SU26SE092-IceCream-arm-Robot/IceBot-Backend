using Application.RobotConfiguration.Storage.Services;
using Application.RobotConfiguration.Storage.Abstractions;
using Application.RobotConfiguration.Artifacts.Abstractions;
using Microsoft.Extensions.Options;
using Minio;
using Minio.DataModel.Args;
using Minio.Exceptions;

namespace Infrastructure.RobotConfiguration.Storage.ObjectStorage;

public sealed class MinioArtifactObjectStorage : IArtifactObjectStorage
{
    private readonly RobotArtifactObjectStorageOptions _options;
    private readonly IMinioClient _client;
    private readonly IMinioClient _downloadClient;
    private readonly ObjectStorageReadResiliencePipeline _readResilience;

    public MinioArtifactObjectStorage(IOptions<RobotArtifactObjectStorageOptions> options)
    {
        _options = options.Value;
        _readResilience = new ObjectStorageReadResiliencePipeline(_options);
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
            await _readResilience.ExecuteAsync(async token =>
            {
                await _client.StatObjectAsync(
                    new StatObjectArgs()
                        .WithBucket(_options.BucketName)
                        .WithObject(storageKey),
                    token);
            }, cancellationToken);
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
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception) when (IsStorageFailure(exception))
        {
            throw new ArtifactObjectStorageUnavailableException(
                $"Artifact object storage could not check '{storageKey}'.",
                exception);
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

        try
        {
            await _client.PutObjectAsync(
                new PutObjectArgs()
                    .WithBucket(_options.BucketName)
                    .WithObject(request.StorageKey)
                    .WithStreamData(content)
                    .WithObjectSize(request.ContentLengthBytes)
                    .WithContentType(request.ContentType),
                cancellationToken);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception) when (IsStorageFailure(exception))
        {
            throw new ArtifactObjectStorageUnavailableException(
                $"Artifact object storage could not write '{request.StorageKey}'.",
                exception);
        }

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
        var url = await _readResilience.ExecuteAsync(async _ =>
            await _downloadClient.PresignedGetObjectAsync(
                new PresignedGetObjectArgs()
                    .WithBucket(_options.BucketName)
                    .WithObject(storageKey)
                    .WithExpiry(expirySeconds)),
            cancellationToken);

        return new ArtifactObjectReadUrlResult(url, expiresAt);
    }

    public async Task<byte[]> ReadBytesAsync(
        string storageKey,
        long maximumBytes,
        CancellationToken cancellationToken = default)
    {
        if (maximumBytes <= 0 || maximumBytes > int.MaxValue)
        {
            throw new ArgumentOutOfRangeException(nameof(maximumBytes));
        }

        try
        {
            return await _readResilience.ExecuteAsync(async token =>
            {
                await using var content = new MemoryStream((int)maximumBytes);
                await _client.GetObjectAsync(
                    new GetObjectArgs()
                        .WithBucket(_options.BucketName)
                        .WithObject(storageKey)
                        .WithCallbackStream(stream => CopyWithLimit(
                            stream,
                            content,
                            storageKey,
                            maximumBytes,
                            token)),
                    token);
                return content.ToArray();
            }, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (ArtifactObjectSizeLimitExceededException)
        {
            throw;
        }
        catch (ObjectNotFoundException exception)
        {
            throw new ArtifactObjectNotFoundException(storageKey, exception);
        }
        catch (BucketNotFoundException exception)
        {
            throw new ArtifactObjectNotFoundException(storageKey, exception);
        }
        catch (Exception exception) when (IsStorageFailure(exception))
        {
            throw new ArtifactObjectStorageUnavailableException(
                $"Artifact object storage could not read '{storageKey}'.",
                exception);
        }
    }

    private static void CopyWithLimit(
        Stream source,
        Stream destination,
        string storageKey,
        long maximumBytes,
        CancellationToken cancellationToken)
    {
        var buffer = new byte[81920];
        long totalBytes = 0;
        while (true)
        {
            var read = source.ReadAsync(buffer.AsMemory(), cancellationToken)
                .AsTask()
                .GetAwaiter()
                .GetResult();
            if (read == 0)
            {
                break;
            }

            totalBytes += read;
            if (totalBytes > maximumBytes)
            {
                throw new ArtifactObjectSizeLimitExceededException(storageKey, maximumBytes);
            }

            destination.Write(buffer, 0, read);
        }
    }

    private static bool IsStorageFailure(Exception exception) =>
        exception is MinioException or HttpRequestException or IOException or TimeoutException;

    public async Task<ArtifactObjectWriteResult> CopyImmutableAsync(
        string sourceStorageKey,
        ArtifactObjectWriteRequest destination,
        CancellationToken cancellationToken = default)
    {
        await EnsureReadyAsync(cancellationToken);
        if (!await ExistsAsync(sourceStorageKey, cancellationToken))
            throw new ArtifactObjectNotFoundException(sourceStorageKey);
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
        var exists = await BucketExistsAsync(cancellationToken);

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
            if (!await BucketExistsAsync(cancellationToken))
            {
                throw;
            }
        }
    }

    private async Task<bool> BucketExistsAsync(CancellationToken cancellationToken)
    {
        return await _readResilience.ExecuteAsync(async token =>
            await _client.BucketExistsAsync(
                new BucketExistsArgs().WithBucket(_options.BucketName),
                token),
            cancellationToken);
    }
}
