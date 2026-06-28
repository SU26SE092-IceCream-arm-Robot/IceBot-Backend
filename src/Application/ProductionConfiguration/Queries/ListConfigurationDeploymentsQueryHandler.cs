using Application.ProductionConfiguration.Abstractions;
using Application.ProductionConfiguration.Results;
using Application.Shared.Wrappers;

namespace Application.ProductionConfiguration.Queries;

public sealed class ListConfigurationDeploymentsQueryHandler
{
    private readonly IProductionConfigurationStore _store;

    public ListConfigurationDeploymentsQueryHandler(IProductionConfigurationStore store) => _store = store;

    public async Task<PagedResult<ConfigurationDeploymentResult>> HandleAsync(
        ListConfigurationDeploymentsQuery query,
        CancellationToken cancellationToken = default)
    {
        var pageNumber = Math.Max(query.PageNumber, 1);
        var pageSize = Math.Clamp(query.PageSize, 1, 100);
        var user = query.UserContext;
        var count = await _store.CountConfigurationDeploymentsAsync(
            query.OrganizationId, query.StoreId, query.KioskId, query.ConfigurationReleaseId,
            query.Profile, query.Status, user.IsSystemAdmin,
            user.AllowedOrganizationIds, user.AllowedStoreIds, user.AllowedKioskIds,
            cancellationToken);
        var deployments = await _store.ListConfigurationDeploymentsAsync(
            query.OrganizationId, query.StoreId, query.KioskId, query.ConfigurationReleaseId,
            query.Profile, query.Status, user.IsSystemAdmin,
            user.AllowedOrganizationIds, user.AllowedStoreIds, user.AllowedKioskIds,
            pageNumber, pageSize, cancellationToken);
        return PagedResult<ConfigurationDeploymentResult>.Success(
            deployments.Select(ConfigurationDeploymentResult.FromReadModel), count, pageNumber, pageSize);
    }
}
