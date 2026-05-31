using Domain.SalesCatalog.Enums;

namespace Application.SalesCatalog.Menus.Requests;

public sealed class UpdateMenuItemRequest
{
    public Guid? ProductId { get; set; }

    public Guid? ProductVariantId { get; set; }

    public Guid? RecipeId { get; set; }

    public string? Code { get; set; }

    public string? DisplayName { get; set; }

    public string? Description { get; set; }

    public MenuItemStatus? Status { get; set; }

    public decimal? Price { get; set; }

    public decimal? DiscountAmount { get; set; }

    public string? Currency { get; set; }

    public int? DisplayOrder { get; set; }

    public int? PreparationTimeSeconds { get; set; }

    public string? ImageUrl { get; set; }

    public DateTimeOffset? EffectiveFrom { get; set; }

    public DateTimeOffset? EffectiveTo { get; set; }

    public int? MetadataSchemaVersion { get; set; }

    public string? MetadataJson { get; set; }
}
