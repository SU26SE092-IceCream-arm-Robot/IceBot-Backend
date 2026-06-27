using Application.ProductionConfiguration.Abstractions;
using Application.ProductionConfiguration.Results;
using Application.Shared.Wrappers;
using Application.Tenants;

namespace Application.ProductionConfiguration.Queries;

public sealed class GetConfigurationReleaseQueryHandler
{
    private readonly IProductionConfigurationStore _store;

    public GetConfigurationReleaseQueryHandler(IProductionConfigurationStore store) => _store = store;

    public async Task<ApiResult<ConfigurationReleaseResult>> HandleAsync(
        GetConfigurationReleaseQuery query,
        CancellationToken cancellationToken = default)
    {
        var release = await _store.GetReleaseByIdAsync(query.ReleaseId, cancellationToken);
        if (release is null)
        {
            return ApiResult<ConfigurationReleaseResult>.Fail("Configuration release not found.", 404);
        }

        if (!ScopeAccessRules.CanAccessScopedRow(query.UserContext, release.OrganizationId, null, null))
        {
            return ApiResult<ConfigurationReleaseResult>.Fail("Access denied.", 403);
        }

        return ApiResult<ConfigurationReleaseResult>.Success(ConfigurationReleaseResult.FromEntity(release));
    }
}
