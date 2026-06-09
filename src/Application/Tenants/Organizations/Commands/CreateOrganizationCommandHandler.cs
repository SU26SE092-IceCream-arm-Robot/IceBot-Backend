using Application.Shared.Wrappers;
using Application.Tenants.Abstractions;
using Application.Tenants.Organizations.Results;
using Domain.Common.Enums;
using Domain.Tenants.Entities;

namespace Application.Tenants.Organizations.Commands;

public sealed class CreateOrganizationCommandHandler
{
    private readonly IOrganizationStore _organizationStore;

    public CreateOrganizationCommandHandler(IOrganizationStore organizationStore)
    {
        _organizationStore = organizationStore;
    }

    public async Task<ApiResult<OrganizationResult>> HandleAsync(
        CreateOrganizationCommand command,
        CancellationToken cancellationToken = default)
    {
        var userContext = command.UserContext;
        var request = command.Request;

        if (!OrganizationAccessRules.CanManageOrganizationLifecycle(userContext))
        {
            return ApiResult<OrganizationResult>.Fail("Only system administrators can create organizations.", 403);
        }

        var code = request.Code.Trim().ToUpperInvariant();
        if (await _organizationStore.ExistsByCodeAsync(code, cancellationToken))
        {
            return ApiResult<OrganizationResult>.Fail($"Organization with code '{code}' already exists.", 409);
        }

        var org = new Organization
        {
            Id = Guid.NewGuid(),
            Code = code,
            Name = request.Name.Trim(),
            LegalName = request.LegalName?.Trim(),
            TaxCode = request.TaxCode?.Trim(),
            Email = request.Email?.Trim().ToLowerInvariant(),
            PhoneNumber = request.PhoneNumber?.Trim(),
            Address = request.Address?.Trim(),
            Status = EntityStatus.Active,
            MetadataJson = request.MetadataJson,
            CreatedAt = DateTimeOffset.UtcNow,
            CreatedByAccountId = userContext.AccountId
        };

        await _organizationStore.AddAsync(org, cancellationToken);
        await _organizationStore.SaveChangesAsync(cancellationToken);

        return ApiResult<OrganizationResult>.Success(OrganizationResultMapper.ToResult(org), "Organization created successfully.", 201);
    }
}
