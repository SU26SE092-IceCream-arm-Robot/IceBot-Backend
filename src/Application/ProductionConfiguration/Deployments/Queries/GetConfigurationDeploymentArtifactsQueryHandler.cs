using Application.ProductionConfiguration.Deployments.Abstractions;
using Application.Shared.Wrappers;
using Application.Tenants;

namespace Application.ProductionConfiguration.Deployments.Queries;

public sealed class GetConfigurationDeploymentArtifactsQueryHandler(
    IConfigurationDeploymentStore store,
    IConfigurationDeploymentArtifactReader reader)
{
    public async Task<ApiResult<IReadOnlyCollection<ConfigurationDeploymentArtifactResult>>> HandleAsync(
        GetConfigurationDeploymentArtifactsQuery query,
        CancellationToken cancellationToken = default)
    {
        var deployment = await store.GetConfigurationDeploymentAsync(query.DeploymentId, cancellationToken);
        if (deployment is null || deployment.KioskId != query.KioskId ||
            !ScopeAccessRules.CanAccessScopedRow(
                ScopeRoleSets.DeploymentRead,
                query.UserContext,
                deployment.OrganizationId,
                deployment.StoreId,
                deployment.KioskId))
        {
            return ApiResult<IReadOnlyCollection<ConfigurationDeploymentArtifactResult>>
                .Fail("Configuration deployment not found.", 404);
        }

        var items = await reader.ListAsync(deployment, cancellationToken);
        return ApiResult<IReadOnlyCollection<ConfigurationDeploymentArtifactResult>>.Success(items);
    }
}
