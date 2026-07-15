using Domain.Common;

namespace Domain.Catalog.Entities;

public partial class ProductCategory : CatalogEntity
{
    public string ProductType { get; set; } = "General";

    public string? ImageUrl { get; set; }

}
