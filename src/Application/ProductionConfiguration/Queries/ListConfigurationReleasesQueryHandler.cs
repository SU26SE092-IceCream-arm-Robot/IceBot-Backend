using Application.ProductionConfiguration.Abstractions;
using Application.ProductionConfiguration.ReadModels;
using Application.Shared.Wrappers;
using Application.Tenants;

namespace Application.ProductionConfiguration.Queries;

public sealed class ListConfigurationReleasesQueryHandler
{
    private readonly IProductionConfigurationStore _store;

    public ListConfigurationReleasesQueryHandler(IProductionConfigurationStore store) => _store = store;

    public async Task<PagedResult<ConfigurationReleaseSummaryReadModel>> HandleAsync(
        ListConfigurationReleasesQuery query,
        CancellationToken cancellationToken = default)
    {
        var pageNumber = Math.Max(query.PageNumber, 1);
        var pageSize = Math.Clamp(query.PageSize, 1, 100);
        var user = query.UserContext;
        if (query.OrganizationId == Guid.Empty)
        {
            return PagedResult<ConfigurationReleaseSummaryReadModel>.Fail(
                "Organization is required.", 400, pageNumber, pageSize);
        }

        if (!ScopeAccessRules.CanAccessScopedRow(
                ScopeRoleSets.ReleaseRead, user, query.OrganizationId, null, null))
        {
            return PagedResult<ConfigurationReleaseSummaryReadModel>.Fail(
                "Access denied.", 403, pageNumber, pageSize);
        }

        var scope = ScopeAccessRules.GetEffectiveScope(ScopeRoleSets.ReleaseRead, user);
        var count = await _store.CountReleasesAsync(
            query.OrganizationId, query.Status, user.IsSystemAdmin, scope.OrganizationIds, cancellationToken);
        var releases = await _store.ListReleasesAsync(
            query.OrganizationId, query.Status, user.IsSystemAdmin, scope.OrganizationIds,
            pageNumber, pageSize, cancellationToken);
        return PagedResult<ConfigurationReleaseSummaryReadModel>.Success(
            releases, count, pageNumber, pageSize);
    }
}
