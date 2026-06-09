using Application.Shared.Wrappers;
using Application.Tenants.Abstractions;
using Application.Tenants.Organizations.Results;

namespace Application.Tenants.Organizations.Queries;

public sealed class ListOrganizationsQueryHandler
{
    private readonly IOrganizationStore _organizationStore;

    public ListOrganizationsQueryHandler(IOrganizationStore organizationStore)
    {
        _organizationStore = organizationStore;
    }

    public async Task<PagedResult<OrganizationResult>> HandleAsync(
        ListOrganizationsQuery query,
        CancellationToken cancellationToken = default)
    {
        var userContext = query.UserContext;
        var search = query.Search;
        var status = query.Status;
        var pageNumber = Math.Max(query.PageNumber, 1);
        var pageSize = Math.Clamp(query.PageSize, 1, 100);

        if (userContext.IsSystemAdmin)
        {
            var total = await _organizationStore.CountAsync(search, status, cancellationToken);
            var list = await _organizationStore.ListAsync(search, status, pageNumber, pageSize, cancellationToken);
            return PagedResult<OrganizationResult>.Success(list.Select(OrganizationResultMapper.ToResult), total, pageNumber, pageSize);
        }
        else
        {
            var total = await _organizationStore.CountByIdsAsync(userContext.AllowedOrganizationIds, search, status, cancellationToken);
            var list = await _organizationStore.ListByIdsAsync(userContext.AllowedOrganizationIds, search, status, pageNumber, pageSize, cancellationToken);
            return PagedResult<OrganizationResult>.Success(list.Select(OrganizationResultMapper.ToResult), total, pageNumber, pageSize);
        }
    }
}
