namespace Application.Catalog.Ingredients.Results;

public sealed class IngredientResult
{
    public Guid Id { get; init; }
    public string Code { get; init; } = null!;
    public string Name { get; init; } = null!;
    public string IngredientType { get; init; } = null!;
    public string Unit { get; init; } = null!;
    public string? Description { get; init; }
    public string? StorageRequirement { get; init; }
    public bool IsPerishable { get; init; }
    public bool IsAllergen { get; init; }
    public int? ShelfLifeDays { get; init; }
    public bool IsActive { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset? UpdatedAt { get; init; }
}
