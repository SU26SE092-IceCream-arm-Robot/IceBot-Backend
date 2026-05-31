using Domain.Common;

namespace Domain.Catalog.Entities;

public partial class Ingredient : BusinessEntity
{
    public string Code { get; set; } = null!;

    public string Name { get; set; } = null!;

    public string IngredientType { get; set; } = "Consumable";

    public string Unit { get; set; } = "gram";

    public string? Description { get; set; }

    public string? StorageRequirement { get; set; }

    public bool IsPerishable { get; set; }

    public bool IsAllergen { get; set; }

    public int? ShelfLifeDays { get; set; }

    public string? MetadataJson { get; set; }
}
