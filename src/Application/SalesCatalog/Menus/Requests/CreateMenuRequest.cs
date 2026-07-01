using Domain.SalesCatalog.Enums;
using Domain.Tenants.Enums;

namespace Application.SalesCatalog.Menus.Requests;

public sealed class CreateMenuRequest
{
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
}
