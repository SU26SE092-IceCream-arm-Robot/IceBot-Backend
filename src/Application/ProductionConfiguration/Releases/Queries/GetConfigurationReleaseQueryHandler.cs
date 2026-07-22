using Application.ProductionConfiguration.Releases.Abstractions;
using Application.ProductionConfiguration.Releases.Results;
using Application.Shared.Wrappers;
using Application.Tenants;

namespace Application.ProductionConfiguration.Releases.Queries;

public sealed class GetConfigurationReleaseQueryHandler
{
    private readonly IConfigurationReleaseStore _store;

    public GetConfigurationReleaseQueryHandler(IConfigurationReleaseStore store) => _store = store;

    public async Task<ApiResult<ConfigurationReleaseResult>> HandleAsync(
        GetConfigurationReleaseQuery query,
        CancellationToken cancellationToken = default)
    {
        var release = await _store.GetReleaseByIdAsync(query.ReleaseId, cancellationToken);
        if (release is null || release.OrganizationId != query.OrganizationId)
        {
            return ApiResult<ConfigurationReleaseResult>.Fail("Configuration release not found.", 404);
        }

        if (!ScopeAccessRules.CanAccessScopedRow(ScopeRoleSets.ReleaseRead, query.UserContext, release.OrganizationId, null, null))
        {
            return ApiResult<ConfigurationReleaseResult>.Fail("Configuration release not found.", 404);
        }

        return ApiResult<ConfigurationReleaseResult>.Success(ConfigurationReleaseResult.FromEntity(release));
    }
}
