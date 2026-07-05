using Domain.SalesCatalog.Enums;
using Domain.Tenants.Enums;

namespace Application.SalesCatalog.Menus.Results;

public sealed class MenuResult
{
    public Guid Id { get; set; }
    public Guid? OrganizationId { get; set; }
    public Guid? StoreId { get; set; }
    public Guid? KioskId { get; set; }
    public string Code { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public MenuStatus Status { get; set; }
    public TenantScopeType ScopeType { get; set; }
    public string Currency { get; set; } = null!;
    public DateTimeOffset? EffectiveFrom { get; set; }
    public DateTimeOffset? EffectiveTo { get; set; }
    public int DisplayOrder { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
    public List<MenuItemResult> Items { get; set; } = new();
}
