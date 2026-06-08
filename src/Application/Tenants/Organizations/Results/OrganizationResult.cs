using System;

namespace Application.Tenants.Organizations.Results;

public sealed class OrganizationResult
{
    public Guid Id { get; set; }
    
    public string Code { get; set; } = null!;
    
    public string Name { get; set; } = null!;
    
    public string? LegalName { get; set; }
    
    public string? TaxCode { get; set; }
    
    public string? Email { get; set; }
    
    public string? PhoneNumber { get; set; }
    
    public string? Address { get; set; }
    
    public string Status { get; set; } = null!;
    
    public string? MetadataJson { get; set; }
    
    public DateTimeOffset CreatedAt { get; set; }
    
    public DateTimeOffset? UpdatedAt { get; set; }
}
