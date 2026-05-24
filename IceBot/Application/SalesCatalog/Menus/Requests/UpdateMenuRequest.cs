using Domain.SalesCatalog.Enums;
using Domain.Tenants.Enums;

namespace Application.SalesCatalog.Menus.Requests;

public sealed class UpdateMenuRequest
{
    public Guid? OrganizationId { get; set; }

    public Guid? StoreId { get; set; }

    public Guid? KioskId { get; set; }

    public string? Code { get; set; }

    public string? Name { get; set; }

    public string? Description { get; set; }

    public MenuStatus? Status { get; set; }

    public TenantScopeType? ScopeType { get; set; }

    public string? Currency { get; set; }

    public DateTimeOffset? EffectiveFrom { get; set; }

    public DateTimeOffset? EffectiveTo { get; set; }

    public int? DisplayOrder { get; set; }

    public int? MetadataSchemaVersion { get; set; }

    public string? MetadataJson { get; set; }
}
