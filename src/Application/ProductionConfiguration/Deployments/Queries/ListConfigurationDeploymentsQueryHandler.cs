using Application.ProductionConfiguration.Deployments.Abstractions;
using Application.ProductionConfiguration.Releases.Results;
using Application.ProductionConfiguration.Deployments.Results;
using Application.Shared.Wrappers;
using Application.Tenants;

namespace Application.ProductionConfiguration.Deployments.Queries;

public sealed class ListConfigurationDeploymentsQueryHandler
{
    private readonly IConfigurationDeploymentStore _store;

    public ListConfigurationDeploymentsQueryHandler(IConfigurationDeploymentStore store) => _store = store;

    public async Task<PagedResult<ConfigurationDeploymentResult>> HandleAsync(
        ListConfigurationDeploymentsQuery query,
        CancellationToken cancellationToken = default)
    {
        var pageNumber = Math.Max(query.PageNumber, 1);
        var pageSize = Math.Clamp(query.PageSize, 1, 100);
        var user = query.UserContext;
        var scope = ScopeAccessRules.GetEffectiveScope(ScopeRoleSets.DeploymentRead, user);
        var count = await _store.CountConfigurationDeploymentsAsync(
            query.OrganizationId, query.StoreId, query.KioskId, query.ConfigurationReleaseId,
            query.Profile, query.Status, user.IsSystemAdmin,
            scope.OrganizationIds, scope.StoreIds, scope.KioskIds,
            cancellationToken);
        var deployments = await _store.ListConfigurationDeploymentsAsync(
            query.OrganizationId, query.StoreId, query.KioskId, query.ConfigurationReleaseId,
            query.Profile, query.Status, user.IsSystemAdmin,
            scope.OrganizationIds, scope.StoreIds, scope.KioskIds,
            pageNumber, pageSize, cancellationToken);
        return PagedResult<ConfigurationDeploymentResult>.Success(
            deployments.Select(ConfigurationDeploymentResult.FromReadModel), count, pageNumber, pageSize);
    }
}
