using Domain.Common;
using Domain.Tenants.Entities;
using Domain.Tenants.Enums;

namespace Domain.Catalog.Entities;

public partial class Product : BusinessEntity, IKioskScoped
{
    public Guid? OrganizationId { get; set; }

    public Guid? StoreId { get; set; }

    public Guid? KioskId { get; set; }

    public Guid? TemplateProductId { get; set; }

    public long? CategoryId { get; set; }

    public string Code { get; set; } = null!;

    public string Name { get; set; } = null!;

    public string? DisplayName { get; set; }

    public string? Description { get; set; }

    public string ProductType { get; set; } = "IceCream";

    public decimal BasePrice { get; set; }

    public string Currency { get; set; } = "VND";

    public bool IsAvailable { get; set; } = true;

    public int? PreparationTimeSeconds { get; set; }

    public string? ImageUrl { get; set; }

    public string? MetadataJson { get; set; }

    public TenantScopeType ScopeType { get; set; } = TenantScopeType.Global;

    public virtual Organization? Organization { get; set; }

    public virtual Store? Store { get; set; }

    public virtual Kiosk? Kiosk { get; set; }

    public virtual Product? TemplateProduct { get; set; }

    public virtual ProductCategory? Category { get; set; }

    public virtual ICollection<ProductVariant> ProductVariants { get; set; } = new List<ProductVariant>();

    public virtual ICollection<ProductOption> ProductOptions { get; set; } = new List<ProductOption>();
}
