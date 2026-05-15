using Domain.Common;

namespace Domain.Entities;

public partial class Product : BusinessEntity
{
    public long? CategoryId { get; set; }

    public string Code { get; set; } = null!;

    public string Name { get; set; } = null!;

    public string? DisplayName { get; set; }

    public string? Description { get; set; }

    public string ProductType { get; set; } = "IceCream";

    public decimal BasePrice { get; set; }

    public string Currency { get; set; } = "VND";

    public bool IsAvailable { get; set; } = true;

    public int? PreparationTimeSeconds { get; set; }

    public string? ImageUrl { get; set; }

    public string? MetadataJson { get; set; }

    public virtual ProductCategory? Category { get; set; }

    public virtual ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();

    public virtual ICollection<Recipe> Recipes { get; set; } = new List<Recipe>();

    public virtual ICollection<ProductOption> ProductOptions { get; set; } = new List<ProductOption>();

    public virtual ICollection<RobotProgram> RobotPrograms { get; set; } = new List<RobotProgram>();
}
