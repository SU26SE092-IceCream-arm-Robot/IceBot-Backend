using Domain.SalesCatalog.Enums;

namespace Application.SalesCatalog.Menus.Requests;

public sealed class CreateMenuItemRequest
{
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
}
