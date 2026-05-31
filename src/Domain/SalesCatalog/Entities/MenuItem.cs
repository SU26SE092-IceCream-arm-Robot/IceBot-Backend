using Domain.Catalog.Entities;
using Domain.Common;
using Domain.SalesCatalog.Enums;

namespace Domain.SalesCatalog.Entities;

public partial class MenuItem : BusinessEntity
{
    public Guid MenuId { get; set; }

    public Guid ProductId { get; set; }

    public Guid ProductVariantId { get; set; }

    public Guid? RecipeId { get; set; }

    public string Code { get; set; } = null!;

    public string DisplayName { get; set; } = null!;

    public string? Description { get; set; }

    public MenuItemStatus Status { get; set; } = MenuItemStatus.Draft;

    public decimal Price { get; set; }

    public decimal DiscountAmount { get; set; }

    public string Currency { get; set; } = "VND";

    public int DisplayOrder { get; set; }

    public int? PreparationTimeSeconds { get; set; }

    public string? ImageUrl { get; set; }

    public DateTimeOffset? EffectiveFrom { get; set; }

    public DateTimeOffset? EffectiveTo { get; set; }

    public int MetadataSchemaVersion { get; set; } = 1;

    public string? MetadataJson { get; set; }

    public virtual Menu Menu { get; set; } = null!;

    public virtual Product Product { get; set; } = null!;

    public virtual ProductVariant ProductVariant { get; set; } = null!;

    public virtual Recipe? Recipe { get; set; }

    public bool IsCurrentlySellable(DateTimeOffset now)
    {
        return Status == MenuItemStatus.Active &&
               (EffectiveFrom is null || EffectiveFrom <= now) &&
               (EffectiveTo is null || EffectiveTo >= now);
    }
}
