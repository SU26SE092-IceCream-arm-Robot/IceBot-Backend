using Application.RobotConfiguration.Storage.Services;
namespace Application.RobotConfiguration.Storage.Abstractions;

public interface IArtifactObjectReferenceSource
{
    Task<IReadOnlyCollection<string>> ListReferencedStorageKeysAsync(
        CancellationToken cancellationToken = default);
}
