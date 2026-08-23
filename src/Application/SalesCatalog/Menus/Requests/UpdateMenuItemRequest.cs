namespace Application.SalesCatalog.Menus.Requests;

public sealed class UpdateMenuItemRequest
{
    public Guid? ProductVariantId { get; set; }

    public Guid? RecipeId { get; set; }

    public bool ClearRecipe { get; set; }

    public string? Code { get; set; }

    public string? DisplayName { get; set; }

    public string? Description { get; set; }

    public decimal? Price { get; set; }

    public decimal? DiscountAmount { get; set; }

    public int? DisplayOrder { get; set; }

    public int? PreparationTimeSeconds { get; set; }

    public DateTimeOffset? EffectiveFrom { get; set; }

    public DateTimeOffset? EffectiveTo { get; set; }

    public List<Guid>? ProductOptionIds { get; set; }
}
