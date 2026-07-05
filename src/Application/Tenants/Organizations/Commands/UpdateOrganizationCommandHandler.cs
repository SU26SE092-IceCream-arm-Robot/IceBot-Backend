using Application.Shared.Wrappers;
using Application.Tenants.Abstractions;
using Application.Tenants.Organizations.Results;

namespace Application.Tenants.Organizations.Commands;

public sealed class UpdateOrganizationCommandHandler
{
    private readonly IOrganizationStore _organizationStore;

    public UpdateOrganizationCommandHandler(IOrganizationStore organizationStore)
    {
        _organizationStore = organizationStore;
    }

    public async Task<ApiResult<OrganizationResult>> HandleAsync(
        UpdateOrganizationCommand command,
        CancellationToken cancellationToken = default)
    {
        var userContext = command.UserContext;
        var organizationId = command.OrganizationId;
        var request = command.Request;

        if (!OrganizationAccessRules.CanAccessOrganization(userContext, organizationId))
        {
            return ApiResult<OrganizationResult>.Fail("Access denied to this organization.", 403);
        }

        var org = await _organizationStore.GetByIdAsync(organizationId, asNoTracking: false, cancellationToken);
        if (org is null)
        {
            return ApiResult<OrganizationResult>.Fail("Organization not found.", 404);
        }

        if (org.DeletedAt is not null)
        {
            return ApiResult<OrganizationResult>.Fail("Cannot update a deleted organization.", 400);
        }

        if (userContext.IsSystemAdmin)
        {
            org.Name = request.Name?.Trim() ?? org.Name;
            org.LegalName = request.LegalName?.Trim() ?? org.LegalName;
            org.TaxCode = request.TaxCode?.Trim() ?? org.TaxCode;
            org.Email = request.Email?.Trim().ToLowerInvariant() ?? org.Email;
            org.PhoneNumber = request.PhoneNumber?.Trim() ?? org.PhoneNumber;
            org.Address = request.Address?.Trim() ?? org.Address;
            org.MetadataJson = request.MetadataJson ?? org.MetadataJson;
        }
        else
        {
            // OrgAdmin can only update basic profile/contact info.
            org.Name = request.Name?.Trim() ?? org.Name;
            org.Email = request.Email?.Trim().ToLowerInvariant() ?? org.Email;
            org.PhoneNumber = request.PhoneNumber?.Trim() ?? org.PhoneNumber;
            org.Address = request.Address?.Trim() ?? org.Address;
        }

        org.UpdatedAt = DateTimeOffset.UtcNow;
        org.UpdatedByAccountId = userContext.AccountId;

        await _organizationStore.SaveChangesAsync(cancellationToken);

        return ApiResult<OrganizationResult>.Success(OrganizationResultMapper.ToResult(org), "Organization updated successfully.");
    }
}
