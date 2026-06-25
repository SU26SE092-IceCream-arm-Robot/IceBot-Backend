using Domain.Devices.Entities;
using Domain.ProductionConfiguration.Entities;

namespace Application.ProductionConfiguration.Abstractions;

public interface IProductionConfigurationStore
{
    Task<ConfigurationRelease?> GetReleaseForPublishAsync(Guid releaseId, CancellationToken cancellationToken = default);

    Task<ConfigurationRelease?> GetPublishedReleaseForDeploymentAsync(Guid releaseId, CancellationToken cancellationToken = default);

    Task<KioskExecutionEndpoint?> GetEndpointForDeploymentAsync(Guid endpointId, CancellationToken cancellationToken = default);

    Task AddFullEdgeDeploymentAsync(KioskConfigurationDeployment deployment, CancellationToken cancellationToken = default);

    Task AddControllerArtifactSetDeploymentAsync(ControllerArtifactSetDeployment deployment, CancellationToken cancellationToken = default);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
