using Domain.Catalog.Enums;

namespace Application.Catalog.Products.Results;

public sealed class OptionGroupResult
{
    public long Id { get; set; }
    public Guid ProductId { get; set; }
    public string Code { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public OptionSelectionType SelectionType { get; set; }
    public int MinSelections { get; set; }
    public int MaxSelections { get; set; }
    public bool IsRequired { get; set; }
    public bool IsActive { get; set; }
    public int DisplayOrder { get; set; }
    public List<ProductOptionResult> Options { get; set; } = new();
}

public sealed class ProductOptionResult
{
    public Guid Id { get; set; }
    public long OptionGroupId { get; set; }
    public string Code { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public decimal PriceDelta { get; set; }
    public string Currency { get; set; } = null!;
    public bool IsDefault { get; set; }
    public bool IsAvailable { get; set; }
    public int DisplayOrder { get; set; }
    public List<ProductOptionIngredientRequirementResult> IngredientRequirements { get; set; } = new();
}

public sealed class ProductOptionIngredientRequirementResult
{
    public Guid IngredientId { get; set; }
    public decimal Quantity { get; set; }
    public string Unit { get; set; } = null!;
    public string RequiredWorkcellCapabilityCode { get; set; } = null!;
}
