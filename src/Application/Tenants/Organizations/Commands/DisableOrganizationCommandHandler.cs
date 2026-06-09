using Application.Shared.Wrappers;
using Application.Tenants.Abstractions;
using Application.Tenants.Organizations.Results;
using Domain.Common.Enums;

namespace Application.Tenants.Organizations.Commands;

public sealed class DisableOrganizationCommandHandler
{
    private readonly IOrganizationStore _organizationStore;

    public DisableOrganizationCommandHandler(IOrganizationStore organizationStore)
    {
        _organizationStore = organizationStore;
    }

    public async Task<ApiResult<OrganizationResult>> HandleAsync(
        DisableOrganizationCommand command,
        CancellationToken cancellationToken = default)
    {
        var userContext = command.UserContext;
        var organizationId = command.OrganizationId;

        if (!OrganizationAccessRules.CanManageOrganizationLifecycle(userContext))
        {
            return ApiResult<OrganizationResult>.Fail("Only system administrators can disable organizations.", 403);
        }

        var org = await _organizationStore.GetByIdAsync(organizationId, asNoTracking: false, cancellationToken);
        if (org is null)
        {
            return ApiResult<OrganizationResult>.Fail("Organization not found.", 404);
        }

        org.Status = EntityStatus.Inactive;
        org.UpdatedAt = DateTimeOffset.UtcNow;
        org.UpdatedByAccountId = userContext.AccountId;

        await _organizationStore.SaveChangesAsync(cancellationToken);

        return ApiResult<OrganizationResult>.Success(OrganizationResultMapper.ToResult(org), "Organization disabled successfully.");
    }
}
