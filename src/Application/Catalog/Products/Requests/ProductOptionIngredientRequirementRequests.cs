namespace Application.Catalog.Products.Requests;

public sealed class ReplaceProductOptionIngredientRequirementsRequest
{
    public List<ProductOptionIngredientRequirementRequest> Items { get; set; } = [];
}

public sealed class ProductOptionIngredientRequirementRequest
{
    public Guid IngredientId { get; set; }
    public decimal Quantity { get; set; }
    public string Unit { get; set; } = null!;
    public string RequiredWorkcellCapabilityCode { get; set; } = null!;
}
