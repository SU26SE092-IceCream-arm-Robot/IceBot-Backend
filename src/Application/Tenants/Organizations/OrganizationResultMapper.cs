using Application.Tenants.Organizations.Results;
using Domain.Tenants.Entities;

namespace Application.Tenants.Organizations;

internal static class OrganizationResultMapper
{
    public static OrganizationResult ToResult(Organization org)
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
