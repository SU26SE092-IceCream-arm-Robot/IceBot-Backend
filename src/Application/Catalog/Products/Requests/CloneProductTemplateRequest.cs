using Domain.Tenants.Enums;

namespace Application.Catalog.Products.Requests;

public sealed class CloneProductTemplateRequest
{
    public Guid TemplateProductId { get; set; }
    public Guid? StoreId { get; set; }
    public Guid? KioskId { get; set; }
    public TenantScopeType ScopeType { get; set; } = TenantScopeType.Organization;
    public string? Code { get; set; }
    public string? Name { get; set; }
}
