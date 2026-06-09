using Application.Shared.Wrappers;
using Application.Tenants.Abstractions;
using Application.Tenants.Organizations.Results;
using Domain.Common.Enums;

namespace Application.Tenants.Organizations.Commands;

public sealed class ActivateOrganizationCommandHandler
{
    private readonly IOrganizationStore _organizationStore;

    public ActivateOrganizationCommandHandler(IOrganizationStore organizationStore)
    {
        _organizationStore = organizationStore;
    }

    public async Task<ApiResult<OrganizationResult>> HandleAsync(
        ActivateOrganizationCommand command,
        CancellationToken cancellationToken = default)
    {
        var userContext = command.UserContext;
        var organizationId = command.OrganizationId;

        if (!OrganizationAccessRules.CanManageOrganizationLifecycle(userContext))
        {
            return ApiResult<OrganizationResult>.Fail("Only system administrators can activate organizations.", 403);
        }

        var org = await _organizationStore.GetByIdAsync(organizationId, asNoTracking: false, cancellationToken);
        if (org is null)
        {
            return ApiResult<OrganizationResult>.Fail("Organization not found.", 404);
        }

        org.Status = EntityStatus.Active;
        org.UpdatedAt = DateTimeOffset.UtcNow;
        org.UpdatedByAccountId = userContext.AccountId;

        await _organizationStore.SaveChangesAsync(cancellationToken);

        return ApiResult<OrganizationResult>.Success(OrganizationResultMapper.ToResult(org), "Organization activated successfully.");
    }
}
