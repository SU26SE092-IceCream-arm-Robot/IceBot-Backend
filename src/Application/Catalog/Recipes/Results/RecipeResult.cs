using Domain.Catalog.Enums;

namespace Application.Catalog.Recipes.Results;

public sealed class RecipeResult
{
    public Guid Id { get; init; }
    public Guid ProductVariantId { get; init; }
    public Guid? TemplateRecipeId { get; init; }
    public string Code { get; init; } = null!;
    public string Name { get; init; } = null!;
    public int Version { get; init; }
    public RecipeStatus Status { get; init; }
    public bool IsDefault { get; init; }
    public decimal YieldQuantity { get; init; }
    public string Unit { get; init; } = null!;
    public int? EstimatedDurationSeconds { get; init; }
    public DateTimeOffset? EffectiveFrom { get; init; }
    public DateTimeOffset? EffectiveTo { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset? UpdatedAt { get; init; }
    public IReadOnlyList<RecipeItemResult> Items { get; init; } = [];
}

public sealed class RecipeItemResult
{
    public Guid Id { get; init; }
    public Guid IngredientId { get; init; }
    public string IngredientCode { get; init; } = null!;
    public string IngredientName { get; init; } = null!;
    public decimal Quantity { get; init; }
    public string Unit { get; init; } = null!;
    public int DisplayOrder { get; init; }
    public bool IsOptional { get; init; }
    public string? Notes { get; init; }
}
