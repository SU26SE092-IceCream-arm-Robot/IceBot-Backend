using Domain.Common;
using Domain.Catalog.Enums;

namespace Domain.Catalog.Entities;

public partial class ProductVariant : BusinessEntity
{
    public Guid ProductId { get; set; }

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

    public virtual Product Product { get; set; } = null!;

    public virtual ICollection<Recipe> Recipes { get; set; } = new List<Recipe>();
}
