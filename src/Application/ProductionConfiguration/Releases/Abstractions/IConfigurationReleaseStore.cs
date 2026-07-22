using Application.ProductionConfiguration.Releases.ReadModels;
using Domain.ProductionConfiguration.Entities;
using Domain.ProductionConfiguration.Enums;

namespace Application.ProductionConfiguration.Releases.Abstractions;

public interface IConfigurationReleaseStore
{
    Task<ConfigurationRelease?> GetReleaseForPublishAsync(Guid releaseId, CancellationToken cancellationToken = default);
    Task<ConfigurationRelease?> GetPublishedReleaseForDeploymentAsync(Guid releaseId, CancellationToken cancellationToken = default);
    Task<ConfigurationRelease?> GetReleaseByIdAsync(Guid releaseId, CancellationToken cancellationToken = default);
    Task<ConfigurationRelease?> GetReleaseForEditAsync(Guid releaseId, CancellationToken cancellationToken = default);
    Task<bool> OrganizationExistsAsync(Guid organizationId, CancellationToken cancellationToken = default);
    Task<ConfigurationRelease> CreateNextReleaseAsync(Guid organizationId, Func<long, ConfigurationRelease> releaseFactory, CancellationToken cancellationToken = default);
    Task<int> CountReleasesAsync(Guid? organizationId, ConfigurationReleaseStatus? status, bool isSystemAdmin, IEnumerable<Guid> allowedOrganizationIds, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ConfigurationReleaseSummaryReadModel>> ListReleasesAsync(Guid? organizationId, ConfigurationReleaseStatus? status, bool isSystemAdmin, IEnumerable<Guid> allowedOrganizationIds, int pageNumber, int pageSize, CancellationToken cancellationToken = default);
    Task<ConfigurationReleaseAuthoringOptionsReadModel> GetAuthoringOptionsAsync(Guid organizationId, Guid? productVariantId, string? search, int limit, CancellationToken cancellationToken = default);
    Task<ConfigurationReleaseDiscardOutcome> DiscardDraftReleaseAsync(ConfigurationRelease release, CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}

public enum ConfigurationReleaseDiscardOutcome
{
    Deleted = 1,
    Referenced = 2
}
