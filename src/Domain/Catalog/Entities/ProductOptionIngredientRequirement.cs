using Domain.Common;

namespace Domain.Catalog.Entities;

/// <summary>Ingredient adjustment selected with a product option.</summary>
public sealed class ProductOptionIngredientRequirement : BusinessEntity
{
    public Guid ProductOptionId { get; set; }
    public Guid IngredientId { get; set; }
    public decimal Quantity { get; set; }
    public string Unit { get; set; } = "gram";
    public string RequiredWorkcellCapabilityCode { get; set; } = null!;

    public ProductOption ProductOption { get; set; } = null!;
}
