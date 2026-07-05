using Domain.SalesCatalog.Enums;

namespace Application.SalesCatalog.Menus.Results;

public sealed class MenuItemResult
{
    public Guid Id { get; set; }
    public Guid MenuId { get; set; }
    public Guid ProductId { get; set; }
    public Guid ProductVariantId { get; set; }
    public Guid? RecipeId { get; set; }
    public string Code { get; set; } = null!;
    public string DisplayName { get; set; } = null!;
    public string? Description { get; set; }
    public MenuItemStatus Status { get; set; }
    public decimal Price { get; set; }
    public decimal DiscountAmount { get; set; }
    public string Currency { get; set; } = null!;
    public int DisplayOrder { get; set; }
    public int? PreparationTimeSeconds { get; set; }
    public string? ImageUrl { get; set; }
    public DateTimeOffset? EffectiveFrom { get; set; }
    public DateTimeOffset? EffectiveTo { get; set; }
    public string? MetadataJson { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
}
