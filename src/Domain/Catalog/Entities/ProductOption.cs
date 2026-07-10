using Domain.Common;
namespace Domain.Catalog.Entities;

public partial class ProductOption : BusinessEntity
{
    public long OptionGroupId { get; set; }

    public Guid? TemplateProductOptionId { get; set; }

    public string Code { get; set; } = null!;

    public string Name { get; set; } = null!;

    public string? Description { get; set; }

    public decimal PriceDelta { get; set; }

    public bool IsDefault { get; set; }

    public bool IsAvailable { get; set; } = true;

    public int DisplayOrder { get; set; }

    public string? MetadataJson { get; set; }

    public virtual OptionGroup OptionGroup { get; set; } = null!;

    public virtual ICollection<ProductOptionIngredientRequirement> IngredientRequirements { get; set; } =
        new List<ProductOptionIngredientRequirement>();
}
