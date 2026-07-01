using Domain.Common;
using Domain.SalesCatalog.Enums;
using Domain.Tenants.Entities;
using Domain.Tenants.Enums;

namespace Domain.SalesCatalog.Entities;

public partial class Menu : BusinessEntity, IKioskScoped
{
    public Guid? OrganizationId { get; set; }

    public Guid? StoreId { get; set; }

    public Guid? KioskId { get; set; }

    public string Code { get; set; } = null!;

    public string Name { get; set; } = null!;

    public string? Description { get; set; }

    public MenuStatus Status { get; set; } = MenuStatus.Draft;

    public TenantScopeType ScopeType { get; set; } = TenantScopeType.Organization;

    public string Currency { get; set; } = "VND";

    public DateTimeOffset? EffectiveFrom { get; set; }

    public DateTimeOffset? EffectiveTo { get; set; }

    public int DisplayOrder { get; set; }

    public int MetadataSchemaVersion { get; set; } = 1;

    public string? MetadataJson { get; set; }

    public virtual Organization? Organization { get; set; }

    public virtual Store? Store { get; set; }

    public virtual Kiosk? Kiosk { get; set; }

    public virtual ICollection<MenuItem> MenuItems { get; set; } = new List<MenuItem>();
}
