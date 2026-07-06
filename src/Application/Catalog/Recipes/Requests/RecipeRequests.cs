using System.ComponentModel.DataAnnotations;
using Domain.Catalog.Enums;

namespace Application.Catalog.Recipes.Requests;

public sealed class CreateRecipeRequest
{
    [Required, StringLength(50, MinimumLength = 2)]
    public string Code { get; set; } = null!;

    [Required, StringLength(200)]
    public string Name { get; set; } = null!;

    [Range(typeof(decimal), "0.000001", "999999999")]
    public decimal YieldQuantity { get; set; } = 1;

    [Required, StringLength(30)]
    public string Unit { get; set; } = "serving";

    [Range(1, 86400)]
    public int? EstimatedDurationSeconds { get; set; }

    public DateTimeOffset? EffectiveFrom { get; set; }
    public DateTimeOffset? EffectiveTo { get; set; }
    public bool IsDefault { get; set; }
}

public sealed class UpdateRecipeRequest
{
    [Required, StringLength(200)]
    public string Name { get; set; } = null!;

    [Range(typeof(decimal), "0.000001", "999999999")]
    public decimal YieldQuantity { get; set; } = 1;

    [Required, StringLength(30)]
    public string Unit { get; set; } = "serving";

    [Range(1, 86400)]
    public int? EstimatedDurationSeconds { get; set; }

    public DateTimeOffset? EffectiveFrom { get; set; }
    public DateTimeOffset? EffectiveTo { get; set; }
    public bool IsDefault { get; set; }
}

public sealed class ReplaceRecipeItemsRequest
{
    [Required, MinLength(1), MaxLength(100)]
    public List<RecipeItemRequest> Items { get; set; } = new();
}

public sealed class RecipeItemRequest
{
    public Guid IngredientId { get; set; }

    [Range(typeof(decimal), "0.000001", "999999999")]
    public decimal Quantity { get; set; }

    [Required, StringLength(30)]
    public string Unit { get; set; } = "gram";

    [Range(1, 1000)]
    public int DisplayOrder { get; set; }

    public bool IsOptional { get; set; }

    [StringLength(500)]
    public string? Notes { get; set; }
}

public sealed class SetRecipeStatusRequest
{
    [EnumDataType(typeof(RecipeStatus))]
    public RecipeStatus Status { get; set; }
}
