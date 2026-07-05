namespace Application.RobotConfiguration.Abstractions;

public interface IArtifactObjectReferenceSource
{
    Task<IReadOnlyCollection<string>> ListReferencedStorageKeysAsync(
        CancellationToken cancellationToken = default);
}
