using Domain.Catalog.Enums;

namespace Application.Catalog.Products.Results;

public sealed class ProductVariantResult
{
    public Guid Id { get; set; }

    public Guid ProductId { get; set; }

    public string Code { get; set; } = null!;

    public string Name { get; set; } = null!;

    public string? DisplayName { get; set; }

    public string? Description { get; set; }

    public string VariantType { get; set; } = null!;

    public FulfillmentType FulfillmentType { get; set; }

    public string? SizeCode { get; set; }

    public decimal BasePrice { get; set; }

    public string Currency { get; set; } = null!;

    public bool IsAvailable { get; set; }

    public int RecipeCount { get; set; }

    public int SellableRecipeCount { get; set; }

    public int DisplayOrder { get; set; }

    public int? PreparationTimeSeconds { get; set; }

    public string? ImageUrl { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset? UpdatedAt { get; set; }
}
