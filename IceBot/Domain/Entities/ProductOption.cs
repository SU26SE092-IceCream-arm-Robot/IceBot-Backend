using Domain.Common;

namespace Domain.Entities;

public partial class ProductOption : BusinessEntity
{
    public long OptionGroupId { get; set; }

    public string Code { get; set; } = null!;

    public string Name { get; set; } = null!;

    public string? Description { get; set; }

    public decimal PriceDelta { get; set; }

    public string Currency { get; set; } = "VND";

    public bool IsDefault { get; set; }

    public bool IsAvailable { get; set; } = true;

    public int DisplayOrder { get; set; }

    public string? MetadataJson { get; set; }

    public virtual OptionGroup OptionGroup { get; set; } = null!;

    public virtual ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();

    public virtual ICollection<Product> Products { get; set; } = new List<Product>();
}
