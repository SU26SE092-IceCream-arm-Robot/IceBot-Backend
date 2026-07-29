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

    public Guid ProductVariantId { get; set; }

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

    public virtual ProductVariant ProductVariant { get; set; } = null!;

    public virtual Recipe? TemplateRecipe { get; set; }

    public virtual ICollection<RecipeItem> RecipeItems { get; set; } = new List<RecipeItem>();

    public void UpdateDraft(
        string name,
        decimal yieldQuantity,
        string unit,
        int? estimatedDurationSeconds,
        DateTimeOffset? effectiveFrom,
        DateTimeOffset? effectiveTo,
        bool isDefault,
        Guid? actorId,
        DateTimeOffset now)
    {
        EnsureDraft();
        if (effectiveFrom.HasValue && effectiveTo.HasValue && effectiveFrom > effectiveTo)
        {
            throw new DomainRuleException("Recipe effectiveFrom must be before effectiveTo.");
        }

        Name = name;
        YieldQuantity = yieldQuantity;
        Unit = unit;
        EstimatedDurationSeconds = estimatedDurationSeconds;
        EffectiveFrom = effectiveFrom;
        EffectiveTo = effectiveTo;
        IsDefault = isDefault;
        UpdatedAt = now;
        UpdatedByAccountId = actorId;
    }

    public void Publish(Guid? actorId, DateTimeOffset now)
    {
        EnsureStatus(RecipeStatus.Draft, "Only Draft recipes can be published.");
        if (RecipeItems.Count == 0 || RecipeItems.All(item => item.IsOptional))
        {
            throw new DomainRuleException("A recipe must contain at least one required ingredient before publication.");
        }

        Status = RecipeStatus.Published;
        UpdatedAt = now;
        UpdatedByAccountId = actorId;
    }

    public void Activate(Guid? actorId, DateTimeOffset now)
    {
        EnsureStatus(RecipeStatus.Published, "Only Published recipes can be activated.");
        Status = RecipeStatus.Active;
        UpdatedAt = now;
        UpdatedByAccountId = actorId;
    }

    public void Retire(Guid? actorId, DateTimeOffset now)
    {
        if (Status == RecipeStatus.Retired)
        {
            return;
        }

        Status = RecipeStatus.Retired;
        UpdatedAt = now;
        UpdatedByAccountId = actorId;
    }

    public void EnsureDraft()
    {
        EnsureStatus(RecipeStatus.Draft, "Only Draft recipes can be edited.");
    }

    private void EnsureStatus(RecipeStatus expected, string message)
    {
        if (Status != expected)
        {
            throw new DomainRuleException(message);
        }
    }
}
