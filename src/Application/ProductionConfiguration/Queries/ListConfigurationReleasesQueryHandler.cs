using Application.ProductionConfiguration.Abstractions;
using Application.ProductionConfiguration.ReadModels;
using Application.Shared.Wrappers;

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
        var count = await _store.CountReleasesAsync(
            query.OrganizationId, query.Status, user.IsSystemAdmin, user.AllowedOrganizationIds, cancellationToken);
        var releases = await _store.ListReleasesAsync(
            query.OrganizationId, query.Status, user.IsSystemAdmin, user.AllowedOrganizationIds,
            pageNumber, pageSize, cancellationToken);
        return PagedResult<ConfigurationReleaseSummaryReadModel>.Success(
            releases, count, pageNumber, pageSize);
    }
}
