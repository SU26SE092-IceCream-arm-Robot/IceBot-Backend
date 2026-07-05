namespace Application.Catalog.Products.Requests;

public sealed class UpdateProductOptionRequest
{
    public string Code { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public decimal PriceDelta { get; set; }
    public bool IsDefault { get; set; }
    public int DisplayOrder { get; set; }
}
