using Domain.Common;
using Domain.Tenants.Entities;
using Domain.Tenants.Enums;

namespace Domain.Catalog.Entities;

public partial class ProductOption : BusinessEntity, IOrganizationScoped
{
    public Guid? OrganizationId { get; set; }

    public long OptionGroupId { get; set; }

    public Guid? TemplateProductOptionId { get; set; }

    public string Code { get; set; } = null!;

    public string Name { get; set; } = null!;

    public string? Description { get; set; }

    public decimal PriceDelta { get; set; }

    public string Currency { get; set; } = "VND";

    public bool IsDefault { get; set; }

    public bool IsAvailable { get; set; } = true;

    public int DisplayOrder { get; set; }

    public string? MetadataJson { get; set; }

    public TenantScopeType ScopeType { get; set; } = TenantScopeType.Global;

    public virtual Organization? Organization { get; set; }

    public virtual ProductOption? TemplateProductOption { get; set; }

    public virtual OptionGroup OptionGroup { get; set; } = null!;
}
