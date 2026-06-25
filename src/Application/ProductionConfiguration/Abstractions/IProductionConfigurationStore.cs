using Domain.Devices.Entities;
using Domain.ProductionConfiguration.Entities;

namespace Application.ProductionConfiguration.Abstractions;

public interface IProductionConfigurationStore
{
    Task<ConfigurationRelease?> GetReleaseForPublishAsync(Guid releaseId, CancellationToken cancellationToken = default);

    Task<ConfigurationRelease?> GetPublishedReleaseForDeploymentAsync(Guid releaseId, CancellationToken cancellationToken = default);

    Task<KioskExecutionEndpoint?> GetEndpointForDeploymentAsync(Guid endpointId, CancellationToken cancellationToken = default);

    Task<bool> HasPendingFullEdgeDeploymentAsync(Guid kioskId, CancellationToken cancellationToken = default);

    Task<int> GetNextFullEdgeDeploymentAttemptNoAsync(
        Guid kioskId,
        Guid configurationReleaseId,
        CancellationToken cancellationToken = default);

    Task<bool> HasPendingControllerArtifactSetDeploymentAsync(Guid controllerId, CancellationToken cancellationToken = default);

    Task<long> GetNextControllerActiveSetVersionAsync(Guid controllerId, CancellationToken cancellationToken = default);

    Task AddFullEdgeDeploymentAsync(KioskConfigurationDeployment deployment, CancellationToken cancellationToken = default);

    Task AddControllerArtifactSetDeploymentAsync(ControllerArtifactSetDeployment deployment, CancellationToken cancellationToken = default);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
