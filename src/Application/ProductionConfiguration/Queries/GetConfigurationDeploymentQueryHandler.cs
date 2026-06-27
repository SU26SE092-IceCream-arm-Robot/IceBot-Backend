using Application.ProductionConfiguration.Abstractions;
using Application.ProductionConfiguration.Results;
using Application.Shared.Wrappers;
using Application.Tenants;

namespace Application.ProductionConfiguration.Queries;

public sealed class GetConfigurationDeploymentQueryHandler
{
    private readonly IProductionConfigurationStore _store;

    public GetConfigurationDeploymentQueryHandler(IProductionConfigurationStore store) => _store = store;

    public async Task<ApiResult<ConfigurationDeploymentResult>> HandleAsync(
        GetConfigurationDeploymentQuery query,
        CancellationToken cancellationToken = default)
    {
        var deployment = await _store.GetConfigurationDeploymentAsync(query.DeploymentId, cancellationToken);
        if (deployment is null)
        {
            return ApiResult<ConfigurationDeploymentResult>.Fail("Configuration deployment not found.", 404);
        }

        if (!ScopeAccessRules.CanAccessScopedRow(
                query.UserContext, deployment.OrganizationId, deployment.StoreId, deployment.KioskId))
        {
            return ApiResult<ConfigurationDeploymentResult>.Fail("Access denied.", 403);
        }

        return ApiResult<ConfigurationDeploymentResult>.Success(
            ConfigurationDeploymentResult.FromReadModel(deployment));
    }
}
