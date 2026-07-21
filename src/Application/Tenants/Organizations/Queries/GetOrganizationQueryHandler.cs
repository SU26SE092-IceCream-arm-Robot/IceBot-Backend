using Application.Shared.Wrappers;
using Application.Tenants.Abstractions;
using Application.Tenants.Organizations.Results;
using Application.Tenants;

namespace Application.Tenants.Organizations.Queries;

public sealed class GetOrganizationQueryHandler
{
    private readonly IOrganizationStore _organizationStore;

    public GetOrganizationQueryHandler(IOrganizationStore organizationStore)
    {
        _organizationStore = organizationStore;
    }

    public async Task<ApiResult<OrganizationResult>> HandleAsync(
        GetOrganizationQuery query,
        CancellationToken cancellationToken = default)
    {
        var userContext = query.UserContext;
        var organizationId = query.OrganizationId;

        if (!OrganizationAccessRules.CanAccessOrganization(
                ScopeRoleSets.OrganizationsView,
                userContext,
                organizationId))
        {
            return ApiResult<OrganizationResult>.Fail("Access denied to this organization.", 403);
        }

        var org = await _organizationStore.GetByIdAsync(organizationId, asNoTracking: true, cancellationToken);
        return org is null
            ? ApiResult<OrganizationResult>.Fail("Organization not found.", 404)
            : ApiResult<OrganizationResult>.Success(OrganizationResultMapper.ToResult(org));
    }
}
