using Application.Identity.Tokens.Claims;
using Application.Shared.Wrappers;
using Application.Tenants.Abstractions;
using Application.Tenants.Organizations.Results;

namespace Application.Tenants.Organizations.Queries;

public sealed class ListOrganizationStatusHistoryQueryHandler
{
    private readonly IOrganizationStore _organizations;

    public ListOrganizationStatusHistoryQueryHandler(IOrganizationStore organizations)
    {
        _organizations = organizations;
    }

    public async Task<ApiResult<IReadOnlyList<OrganizationStatusTransitionResult>>> HandleAsync(
        CurrentUserContext userContext,
        Guid organizationId,
        CancellationToken cancellationToken = default)
    {
        if (!OrganizationAccessRules.CanManageOrganizationLifecycle(userContext))
        {
            return ApiResult<IReadOnlyList<OrganizationStatusTransitionResult>>.Fail(
                "Only system administrators can view organization lifecycle history.", 403);
        }

        var organization = await _organizations.GetByIdAsync(organizationId, true, cancellationToken);
        if (organization is null)
        {
            return ApiResult<IReadOnlyList<OrganizationStatusTransitionResult>>.Fail("Organization not found.", 404);
        }

        var transitions = await _organizations.ListStatusTransitionsAsync(organizationId, cancellationToken);
        var result = transitions.Select(transition => new OrganizationStatusTransitionResult
        {
            Id = transition.Id,
            FromStatus = transition.FromStatus.ToString(),
            ToStatus = transition.ToStatus.ToString(),
            ReasonCode = transition.ReasonCode,
            Reason = transition.Reason,
            ChangedByAccountId = transition.ChangedByAccountId,
            ChangedAt = transition.ChangedAt,
            OrganizationStatusRevision = transition.OrganizationStatusRevision,
            ReadinessConfirmed = transition.ReadinessConfirmed
        }).ToArray();
        return ApiResult<IReadOnlyList<OrganizationStatusTransitionResult>>.Success(result);
    }
}
