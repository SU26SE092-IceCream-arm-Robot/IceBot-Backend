using Domain.Catalog.Enums;
using Domain.Common;
using Domain.Tenants.Entities;
using Domain.Tenants.Enums;

namespace Domain.Catalog.Entities;

public partial class Recipe : BusinessEntity, IKioskScoped
{
    public Guid? OrganizationId { get; set; }

    public Guid? StoreId { get; set; }

    public Guid? KioskId { get; set; }

    public Guid ProductId { get; set; }

    public Guid? TemplateRecipeId { get; set; }

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

    public int InstructionsSchemaVersion { get; set; } = 1;

    public string? InstructionsJson { get; set; }

    public TenantScopeType ScopeType { get; set; } = TenantScopeType.Global;

    public virtual Organization? Organization { get; set; }

    public virtual Store? Store { get; set; }

    public virtual Kiosk? Kiosk { get; set; }

    public virtual Product Product { get; set; } = null!;

    public virtual Recipe? TemplateRecipe { get; set; }

    public virtual ICollection<RecipeItem> RecipeItems { get; set; } = new List<RecipeItem>();
}
