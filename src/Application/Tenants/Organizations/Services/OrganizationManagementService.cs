using Application.Identity.Tokens.Claims;
using Application.Shared.Wrappers;
using Application.Tenants.Abstractions;
using Application.Tenants.Organizations.Requests;
using Application.Tenants.Organizations.Results;
using Domain.Common.Enums;
using Domain.Tenants.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Application.Tenants.Organizations.Services;

public sealed class OrganizationManagementService
{
    private readonly IOrganizationStore _organizationStore;

    public OrganizationManagementService(IOrganizationStore organizationStore)
    {
        _organizationStore = organizationStore;
    }

    public async Task<PagedResult<OrganizationResult>> ListOrganizationsAsync(
        CurrentUserContext userContext,
        string? search,
        string? status,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        pageNumber = Math.Max(pageNumber, 1);
        pageSize = Math.Clamp(pageSize, 1, 100);

        if (userContext.IsSystemAdmin)
        {
            var total = await _organizationStore.CountAsync(search, status, cancellationToken);
            var list = await _organizationStore.ListAsync(search, status, pageNumber, pageSize, cancellationToken);
            return PagedResult<OrganizationResult>.Success(list.Select(ToResult), total, pageNumber, pageSize);
        }
        else
        {
            var total = await _organizationStore.CountByIdsAsync(userContext.AllowedOrganizationIds, search, status, cancellationToken);
            var list = await _organizationStore.ListByIdsAsync(userContext.AllowedOrganizationIds, search, status, pageNumber, pageSize, cancellationToken);
            return PagedResult<OrganizationResult>.Success(list.Select(ToResult), total, pageNumber, pageSize);
        }
    }

    public async Task<ApiResult<OrganizationResult>> GetOrganizationAsync(
        CurrentUserContext userContext,
        Guid organizationId,
        CancellationToken cancellationToken = default)
    {
        if (!userContext.IsSystemAdmin && !userContext.AllowedOrganizationIds.Contains(organizationId))
        {
            return ApiResult<OrganizationResult>.Fail("Access denied to this organization.", 403);
        }

        var org = await _organizationStore.GetByIdAsync(organizationId, asNoTracking: true, cancellationToken);
        return org is null
            ? ApiResult<OrganizationResult>.Fail("Organization not found.", 404)
            : ApiResult<OrganizationResult>.Success(ToResult(org));
    }

    public async Task<ApiResult<OrganizationResult>> CreateOrganizationAsync(
        CurrentUserContext userContext,
        CreateOrganizationRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!userContext.IsSystemAdmin)
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

        return ApiResult<OrganizationResult>.Success(ToResult(org), "Organization created successfully.", 201);
    }

    public async Task<ApiResult<OrganizationResult>> UpdateOrganizationAsync(
        CurrentUserContext userContext,
        Guid organizationId,
        UpdateOrganizationRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!userContext.IsSystemAdmin && !userContext.AllowedOrganizationIds.Contains(organizationId))
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
            org.Status = request.Status ?? org.Status;
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

        return ApiResult<OrganizationResult>.Success(ToResult(org), "Organization updated successfully.");
    }

    public async Task<ApiResult<OrganizationResult>> DisableOrganizationAsync(
        CurrentUserContext userContext,
        Guid organizationId,
        CancellationToken cancellationToken = default)
    {
        if (!userContext.IsSystemAdmin)
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

        return ApiResult<OrganizationResult>.Success(ToResult(org), "Organization disabled successfully.");
    }

    public async Task<ApiResult<OrganizationResult>> ActivateOrganizationAsync(
        CurrentUserContext userContext,
        Guid organizationId,
        CancellationToken cancellationToken = default)
    {
        if (!userContext.IsSystemAdmin)
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

        return ApiResult<OrganizationResult>.Success(ToResult(org), "Organization activated successfully.");
    }

    private static OrganizationResult ToResult(Organization org)
    {
        return new OrganizationResult
        {
            Id = org.Id,
            Code = org.Code,
            Name = org.Name,
            LegalName = org.LegalName,
            TaxCode = org.TaxCode,
            Email = org.Email,
            PhoneNumber = org.PhoneNumber,
            Address = org.Address,
            Status = org.Status.ToString(),
            MetadataJson = org.MetadataJson,
            CreatedAt = org.CreatedAt,
            UpdatedAt = org.UpdatedAt
        };
    }
}
