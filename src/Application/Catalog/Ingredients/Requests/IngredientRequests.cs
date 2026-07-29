using System.ComponentModel.DataAnnotations;

namespace Application.Catalog.Ingredients.Requests;

public sealed class CreateIngredientRequest
{
    [Required, StringLength(50, MinimumLength = 2)]
    public string Code { get; set; } = null!;

    [Required, StringLength(200)]
    public string Name { get; set; } = null!;

    [Required, StringLength(50)]
    public string IngredientType { get; set; } = "Consumable";

    [Required, StringLength(30)]
    public string Unit { get; set; } = "gram";

    [StringLength(1000)]
    public string? Description { get; set; }

    [StringLength(200)]
    public string? StorageRequirement { get; set; }

    public bool IsPerishable { get; set; }
    public bool IsAllergen { get; set; }

    [Range(1, 36500)]
    public int? ShelfLifeDays { get; set; }
}

public sealed class UpdateIngredientRequest
{
    [Required, StringLength(200)]
    public string Name { get; set; } = null!;

    [Required, StringLength(50)]
    public string IngredientType { get; set; } = "Consumable";

    [Required, StringLength(30)]
    public string Unit { get; set; } = "gram";

    [StringLength(1000)]
    public string? Description { get; set; }

    [StringLength(200)]
    public string? StorageRequirement { get; set; }

    public bool IsPerishable { get; set; }
    public bool IsAllergen { get; set; }

    [Range(1, 36500)]
    public int? ShelfLifeDays { get; set; }
}
