using Domain.Catalog.Enums;

namespace Application.Catalog.Products.Requests;

public sealed class UpdateProductVariantRequest
{
    public string? Code { get; set; }

    public string? Name { get; set; }

    public string? DisplayName { get; set; }

    public string? Description { get; set; }

    public string? VariantType { get; set; }

    public FulfillmentType? FulfillmentType { get; set; }

    public string? SizeCode { get; set; }

    public decimal? BasePrice { get; set; }

    public int? DisplayOrder { get; set; }

    public int? PreparationTimeSeconds { get; set; }

}
