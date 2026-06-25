using Domain.Catalog.Enums;

namespace Application.Catalog.Products.Requests;

public sealed class UpsertProductVariantRequest
{
    public string Code { get; set; } = null!;

    public string Name { get; set; } = null!;

    public string? DisplayName { get; set; }

    public string? Description { get; set; }

    public string VariantType { get; set; } = "Default";

    public FulfillmentType FulfillmentType { get; set; } = FulfillmentType.Packaged;

    public string? SizeCode { get; set; }

    public decimal BasePrice { get; set; }

    public string Currency { get; set; } = "VND";

    public bool IsAvailable { get; set; } = true;

    public int DisplayOrder { get; set; }

    public int? PreparationTimeSeconds { get; set; }

    public string? ImageUrl { get; set; }

    public string? MetadataJson { get; set; }
}
