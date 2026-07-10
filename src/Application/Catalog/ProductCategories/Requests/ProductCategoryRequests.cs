using System.ComponentModel.DataAnnotations;

namespace Application.Catalog.ProductCategories.Requests;

public sealed class CreateProductCategoryRequest
{
    [Required, StringLength(50, MinimumLength = 2)]
    public string Code { get; set; } = null!;

    [Required, StringLength(200)]
    public string Name { get; set; } = null!;

    [StringLength(1000)]
    public string? Description { get; set; }

    [Required, StringLength(50)]
    public string ProductType { get; set; } = "General";

    [StringLength(1000)]
    public string? ImageUrl { get; set; }

    [Range(0, int.MaxValue)]
    public int DisplayOrder { get; set; }
}

public sealed class UpdateProductCategoryRequest
{
    [Required, StringLength(200)]
    public string Name { get; set; } = null!;

    [StringLength(1000)]
    public string? Description { get; set; }

    [Required, StringLength(50)]
    public string ProductType { get; set; } = "General";

    [StringLength(1000)]
    public string? ImageUrl { get; set; }

    [Range(0, int.MaxValue)]
    public int DisplayOrder { get; set; }
}

public sealed class SetProductCategoryStatusRequest
{
    public bool IsActive { get; set; }
}
