using Application.ProductionConfiguration.Deployments.Abstractions;
using Application.ProductionConfiguration.Releases.Results;
using Application.ProductionConfiguration.Deployments.Results;
using Application.Shared.Wrappers;
using Application.Tenants;

namespace Application.ProductionConfiguration.Deployments.Queries;

public sealed class GetConfigurationDeploymentQueryHandler
{
    private readonly IConfigurationDeploymentStore _store;

    public GetConfigurationDeploymentQueryHandler(IConfigurationDeploymentStore store) => _store = store;

    public async Task<ApiResult<ConfigurationDeploymentResult>> HandleAsync(
        GetConfigurationDeploymentQuery query,
        CancellationToken cancellationToken = default)
    {
        var deployment = await _store.GetConfigurationDeploymentAsync(query.DeploymentId, cancellationToken);
        if (deployment is null)
        {
            return ApiResult<ConfigurationDeploymentResult>.Fail("Configuration deployment not found.", 404);
        }

        if (deployment.KioskId != query.KioskId)
        {
            return ApiResult<ConfigurationDeploymentResult>.Fail("Configuration deployment not found.", 404);
        }

        if (!ScopeAccessRules.CanAccessScopedRow(ScopeRoleSets.DeploymentRead,
                query.UserContext, deployment.OrganizationId, deployment.StoreId, deployment.KioskId))
        {
            return ApiResult<ConfigurationDeploymentResult>.Fail("Configuration deployment not found.", 404);
        }

        return ApiResult<ConfigurationDeploymentResult>.Success(
            ConfigurationDeploymentResult.FromReadModel(deployment));
    }
}
