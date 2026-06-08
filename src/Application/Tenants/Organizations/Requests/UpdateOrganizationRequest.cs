using Domain.Common.Enums;
using System.ComponentModel.DataAnnotations;

namespace Application.Tenants.Organizations.Requests;

public sealed class UpdateOrganizationRequest
{
    [StringLength(200)]
    public string? Name { get; set; }

    public string? LegalName { get; set; }
    
    public string? TaxCode { get; set; }

    [EmailAddress]
    public string? Email { get; set; }

    [Phone]
    public string? PhoneNumber { get; set; }

    public string? Address { get; set; }
    
    public EntityStatus? Status { get; set; }
    
    public string? MetadataJson { get; set; }
}
