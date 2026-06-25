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

        _client = new MinioClient()
            .WithEndpoint(_options.Endpoint)
            .WithCredentials(_options.AccessKey, _options.SecretKey)
            .WithSSL(_options.UseSsl)
            .Build();
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
    }

    public async Task<ArtifactObjectWriteResult> WriteImmutableAsync(
        ArtifactObjectWriteRequest request,
        Stream content,
        CancellationToken cancellationToken = default)
    {
        await EnsureBucketAsync(cancellationToken);

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

    private async Task EnsureBucketAsync(CancellationToken cancellationToken)
    {
        var exists = await _client.BucketExistsAsync(
            new BucketExistsArgs().WithBucket(_options.BucketName),
            cancellationToken);

        if (!exists)
        {
            await _client.MakeBucketAsync(
                new MakeBucketArgs().WithBucket(_options.BucketName),
                cancellationToken);
        }
    }
}
