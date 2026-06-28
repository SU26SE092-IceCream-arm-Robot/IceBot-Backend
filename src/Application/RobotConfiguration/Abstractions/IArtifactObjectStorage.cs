namespace Application.RobotConfiguration.Abstractions;

public interface IArtifactObjectStorage
{
    Task<bool> ExistsAsync(string storageKey, CancellationToken cancellationToken = default);

    Task<ArtifactObjectWriteResult> WriteImmutableAsync(
        ArtifactObjectWriteRequest request,
        Stream content,
        CancellationToken cancellationToken = default);

    Task<ArtifactObjectReadUrlResult> CreateReadUrlAsync(
        string storageKey,
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
