using Domain.Common;
using Domain.Enums;

namespace Domain.Entities;

public partial class Recipe : BusinessEntity
{
    public Guid ProductId { get; set; }

    public string Code { get; set; } = null!;

    public string Name { get; set; } = null!;

    public int Version { get; set; } = 1;

    public RecipeStatus Status { get; set; } = RecipeStatus.Draft;

    public bool IsDefault { get; set; }

    public decimal YieldQuantity { get; set; } = 1;

    public string Unit { get; set; } = "serving";

    public int? EstimatedDurationSeconds { get; set; }

    public DateTimeOffset? EffectiveFrom { get; set; }

    public DateTimeOffset? EffectiveTo { get; set; }

    public string? InstructionsJson { get; set; }

    public virtual Product Product { get; set; } = null!;

    public virtual ICollection<RecipeItem> RecipeItems { get; set; } = new List<RecipeItem>();
}
