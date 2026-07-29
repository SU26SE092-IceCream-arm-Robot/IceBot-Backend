namespace Application.Catalog.Products.Requests;

using Domain.Catalog.Enums;

public sealed class CreateProductOptionRequest
{
    public string Code { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public decimal PriceDelta { get; set; }
    public required ProductOptionExecutionImpact ExecutionImpact { get; set; }
    public bool IsDefault { get; set; }
    public int DisplayOrder { get; set; }
}
