using Domain.Common;

namespace Domain.Catalog.Entities;

public partial class ProductCategory : CatalogEntity
{
    public long? ParentCategoryId { get; set; }

    public string ProductType { get; set; } = "General";

    public string? ImageUrl { get; set; }

    public virtual ProductCategory? ParentCategory { get; set; }

    public virtual ICollection<ProductCategory> ChildCategories { get; set; } = new List<ProductCategory>();
}
