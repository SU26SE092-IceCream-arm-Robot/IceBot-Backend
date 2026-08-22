namespace Application.SalesCatalog.RuntimeMenus.Results;

public sealed class RuntimeMenuItemResult
{
    public Guid MenuId { get; set; }

    public Guid MenuItemId { get; set; }

    public Guid ProductId { get; set; }

    public Guid ProductVariantId { get; set; }

    public Guid? RecipeId { get; set; }

    public string MenuItemCode { get; set; } = null!;

    public string ProductCode { get; set; } = null!;

    public string ProductVariantCode { get; set; } = null!;

    public string DisplayName { get; set; } = null!;

    public string? Description { get; set; }

    public string? SizeCode { get; set; }

    public decimal Price { get; set; }

    public decimal DiscountAmount { get; set; }

    public decimal FinalPrice { get; set; }

    public string Currency { get; set; } = null!;

    public int? PreparationTimeSeconds { get; set; }

    public string? ImageUrl { get; set; }

    public int? RecipeVersion { get; set; }

    public List<RuntimeMenuOptionGroupResult> OptionGroups { get; set; } = new();
}

public sealed class RuntimeMenuOptionGroupResult
{
    public long OptionGroupId { get; set; }
    public string Code { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string SelectionType { get; set; } = null!;
    public int MinSelections { get; set; }
    public int MaxSelections { get; set; }
    public bool IsRequired { get; set; }
    public List<RuntimeMenuProductOptionResult> Options { get; set; } = new();
}

public sealed class RuntimeMenuProductOptionResult
{
    public Guid ProductOptionId { get; set; }
    public string Code { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public decimal PriceDelta { get; set; }
    public string Currency { get; set; } = null!;
    public bool IsDefault { get; set; }
    public string ExecutionImpact { get; set; } = null!;
}
