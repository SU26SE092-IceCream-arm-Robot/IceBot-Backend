using Application.ProductionConfiguration.Abstractions;
using Application.ProductionConfiguration.ReadModels;
using Application.Shared.Wrappers;
using Application.Tenants;

namespace Application.ProductionConfiguration.Queries;

public sealed class GetConfigurationReleaseAuthoringOptionsQueryHandler
{
    private readonly IProductionConfigurationStore _store;
    public GetConfigurationReleaseAuthoringOptionsQueryHandler(IProductionConfigurationStore store) => _store = store;

    public async Task<ApiResult<ConfigurationReleaseAuthoringOptionsReadModel>> HandleAsync(
        GetConfigurationReleaseAuthoringOptionsQuery query,
        CancellationToken cancellationToken = default)
    {
        if (query.OrganizationId == Guid.Empty)
            return ApiResult<ConfigurationReleaseAuthoringOptionsReadModel>.Fail("Organization is required.", 400);
        if (!ScopeAccessRules.CanAccessScopedRow(ScopeRoleSets.ReleaseRead, query.UserContext, query.OrganizationId, null, null))
            return ApiResult<ConfigurationReleaseAuthoringOptionsReadModel>.Fail("Access denied.", 403);
        if (!await _store.OrganizationExistsAsync(query.OrganizationId, cancellationToken))
            return ApiResult<ConfigurationReleaseAuthoringOptionsReadModel>.Fail("Organization not found.", 404);

        var result = await _store.GetAuthoringOptionsAsync(
            query.OrganizationId,
            query.ProductVariantId,
            query.Search,
            Math.Clamp(query.Limit, 1, 100),
            cancellationToken);
        return ApiResult<ConfigurationReleaseAuthoringOptionsReadModel>.Success(result);
    }
}
