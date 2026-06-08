using System.ComponentModel.DataAnnotations;

namespace Application.Tenants.Organizations.Requests;

public sealed class CreateOrganizationRequest
{
    [Required]
    [StringLength(50, MinimumLength = 2)]
    public string Code { get; set; } = null!;

    [Required]
    [StringLength(200)]
    public string Name { get; set; } = null!;

    public string? LegalName { get; set; }
    
    public string? TaxCode { get; set; }

    [EmailAddress]
    public string? Email { get; set; }

    [Phone]
    public string? PhoneNumber { get; set; }

    public string? Address { get; set; }
    
    public string? MetadataJson { get; set; }
}
