using Domain.Devices.Entities;
using Domain.ProductionConfiguration.Entities;
using Domain.ProductionConfiguration.Enums;
using Domain.Catalog.Entities;
using Domain.RobotConfiguration.Entities;

namespace Application.ProductionConfiguration.Abstractions;

public interface IProductionConfigurationStore
{
    Task<ConfigurationRelease?> GetReleaseForPublishAsync(Guid releaseId, CancellationToken cancellationToken = default);

    Task<ConfigurationRelease?> GetPublishedReleaseForDeploymentAsync(Guid releaseId, CancellationToken cancellationToken = default);

    Task<ConfigurationRelease?> GetReleaseByIdAsync(Guid releaseId, CancellationToken cancellationToken = default);

    Task<ConfigurationRelease?> GetReleaseForEditAsync(Guid releaseId, CancellationToken cancellationToken = default);

    Task<bool> OrganizationExistsAsync(Guid organizationId, CancellationToken cancellationToken = default);

    Task<long> GetNextReleaseNumberAsync(Guid organizationId, CancellationToken cancellationToken = default);

    Task<int> CountReleasesAsync(
        Guid? organizationId,
        ConfigurationReleaseStatus? status,
        bool isSystemAdmin,
        IEnumerable<Guid> allowedOrganizationIds,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ConfigurationRelease>> ListReleasesAsync(
        Guid? organizationId,
        ConfigurationReleaseStatus? status,
        bool isSystemAdmin,
        IEnumerable<Guid> allowedOrganizationIds,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ProductVariant>> ListProductVariantsByIdsAsync(
        IReadOnlyCollection<Guid> ids,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Recipe>> ListRecipesByIdsAsync(
        IReadOnlyCollection<Guid> ids,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<RobotProgram>> ListRobotProgramsByIdsAsync(
        IReadOnlyCollection<Guid> ids,
        CancellationToken cancellationToken = default);

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

    Task AddReleaseAsync(ConfigurationRelease release, CancellationToken cancellationToken = default);

    void DeleteReleaseRoutes(IEnumerable<ExecutionRoute> routes);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
