namespace Application.Catalog.ProductCategories.Results;

public sealed class ProductCategoryResult
{
    public long Id { get; init; }
    public string Code { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public string ProductType { get; init; } = string.Empty;
    public string? ImageUrl { get; init; }
    public bool IsActive { get; init; }
    public int DisplayOrder { get; init; }
}
