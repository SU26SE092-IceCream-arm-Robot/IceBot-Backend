using Application.RobotConfiguration.Storage.Services;
namespace Application.RobotConfiguration.Storage.Abstractions;

public interface IArtifactObjectStorage
{
    Task EnsureReadyAsync(CancellationToken cancellationToken = default);

    Task<bool> ExistsAsync(string storageKey, CancellationToken cancellationToken = default);

    Task<ArtifactObjectWriteResult> WriteImmutableAsync(
        ArtifactObjectWriteRequest request,
        Stream content,
        CancellationToken cancellationToken = default);

    Task<ArtifactObjectReadUrlResult> CreateReadUrlAsync(
        string storageKey,
        CancellationToken cancellationToken = default);

    Task<byte[]> ReadBytesAsync(
        string storageKey,
        long maximumBytes,
        CancellationToken cancellationToken = default);

    Task<ArtifactObjectWriteResult> CopyImmutableAsync(
        string sourceStorageKey,
        ArtifactObjectWriteRequest destination,
        CancellationToken cancellationToken = default);

    IAsyncEnumerable<ArtifactObjectInfo> ListAsync(
        string prefix,
        CancellationToken cancellationToken = default);

    Task DeleteIfExistsAsync(string storageKey, CancellationToken cancellationToken = default);
}

public sealed record ArtifactObjectWriteRequest(
    string StorageKey,
    string ContentType,
    long ContentLengthBytes,
    string Checksum);

public sealed record ArtifactObjectWriteResult(string StorageKey, string Checksum, long ContentLengthBytes);

public sealed record ArtifactObjectReadUrlResult(string Url, DateTimeOffset ExpiresAt);

public sealed record ArtifactObjectInfo(string StorageKey, DateTimeOffset LastModifiedAt, long SizeBytes);

public sealed class ArtifactObjectAlreadyExistsException : Exception
{
    public ArtifactObjectAlreadyExistsException(string storageKey)
        : base($"Artifact object already exists: {storageKey}")
    {
        StorageKey = storageKey;
    }

    public string StorageKey { get; }
}

public sealed class ArtifactObjectSizeLimitExceededException : Exception
{
    public ArtifactObjectSizeLimitExceededException(string storageKey, long maximumBytes)
        : base($"Artifact object '{storageKey}' exceeds the {maximumBytes}-byte read limit.")
    {
    }
}

public sealed class ArtifactObjectNotFoundException : Exception
{
    public ArtifactObjectNotFoundException(string storageKey, Exception? innerException = null)
        : base($"Artifact object was not found: {storageKey}", innerException)
    {
        StorageKey = storageKey;
    }

    public string StorageKey { get; }
}

public sealed class ArtifactObjectIntegrityException : Exception
{
    public ArtifactObjectIntegrityException(string storageKey, string message)
        : base(message)
    {
        StorageKey = storageKey;
    }

    public string StorageKey { get; }
}

public sealed class ArtifactObjectStorageUnavailableException : Exception
{
    public ArtifactObjectStorageUnavailableException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
