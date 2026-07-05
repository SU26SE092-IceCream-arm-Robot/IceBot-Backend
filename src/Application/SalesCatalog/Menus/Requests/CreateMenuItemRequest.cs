using Domain.SalesCatalog.Enums;

namespace Application.SalesCatalog.Menus.Requests;

public sealed class CreateMenuItemRequest
{
    public Guid ProductVariantId { get; set; }

    public Guid? RecipeId { get; set; }

    public string Code { get; set; } = null!;

    public string DisplayName { get; set; } = null!;

    public string? Description { get; set; }

    public decimal Price { get; set; }

    public decimal DiscountAmount { get; set; }

    public int DisplayOrder { get; set; }

    public int? PreparationTimeSeconds { get; set; }

    public string? ImageUrl { get; set; }

    public DateTimeOffset? EffectiveFrom { get; set; }

    public DateTimeOffset? EffectiveTo { get; set; }

}
