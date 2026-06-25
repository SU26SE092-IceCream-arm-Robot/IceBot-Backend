namespace Application.RobotConfiguration.Abstractions;

public interface IArtifactObjectStorage
{
    Task<bool> ExistsAsync(string storageKey, CancellationToken cancellationToken = default);

    Task<ArtifactObjectWriteResult> WriteImmutableAsync(
        ArtifactObjectWriteRequest request,
        Stream content,
        CancellationToken cancellationToken = default);
}

public sealed record ArtifactObjectWriteRequest(
    string StorageKey,
    string ContentType,
    long ContentLengthBytes,
    string Checksum);

public sealed record ArtifactObjectWriteResult(string StorageKey, string Checksum, long ContentLengthBytes);

public sealed class ArtifactObjectAlreadyExistsException : Exception
{
    public ArtifactObjectAlreadyExistsException(string storageKey)
        : base($"Artifact object already exists: {storageKey}")
    {
        StorageKey = storageKey;
    }

    public string StorageKey { get; }
}
