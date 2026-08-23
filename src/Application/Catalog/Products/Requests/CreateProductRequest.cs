namespace Application.Catalog.Products.Requests;

public sealed class CreateProductRequest
{
    public Guid? StoreId { get; set; }

    public Guid? KioskId { get; set; }

    public long? CategoryId { get; set; }

    public string Code { get; set; } = null!;

    public string Name { get; set; } = null!;

    public string? DisplayName { get; set; }

    public string? Description { get; set; }

    public string ProductType { get; set; } = "IceCream";

    public decimal BasePrice { get; set; }

    public string Currency { get; set; } = "VND";

    public int? PreparationTimeSeconds { get; set; }

    public List<UpsertProductVariantRequest> Variants { get; set; } = new();
}
