using Domain.Common;

namespace Domain.Catalog.Entities;

public partial class RecipeItem : BusinessEntity
{
    public Guid RecipeId { get; set; }

    public Guid IngredientId { get; set; }

    public decimal Quantity { get; set; }

    public string Unit { get; set; } = "gram";

    public int StepOrder { get; set; }

    public bool IsOptional { get; set; }

    public string? Notes { get; set; }

    public virtual Ingredient Ingredient { get; set; } = null!;

    public virtual Recipe Recipe { get; set; } = null!;
}
