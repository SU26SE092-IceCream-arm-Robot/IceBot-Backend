using Domain.Catalog.Enums;

namespace Application.SalesCatalog.ReadModels;

public sealed record MenuItemProductOptionReadModel(
    Guid MenuItemId,
    Guid ProductOptionId,
    long OptionGroupId,
    string OptionGroupCode,
    string OptionGroupName,
    OptionSelectionType SelectionType,
    int MinSelections,
    int MaxSelections,
    bool IsRequired,
    string Code,
    string Name,
    string? Description,
    decimal PriceDelta,
    ProductOptionExecutionImpact ExecutionImpact,
    bool IsAvailable,
    bool AreIngredientRequirementsActive,
    bool IsDefault,
    int DisplayOrder);

public sealed record ActiveProductionRouteOptionPolicy(
    Guid ExecutionRouteId,
    IReadOnlySet<string> SupportedOptionCodes);

public readonly record struct ActiveProductionRouteOptionPolicyKey(
    Guid ProductVariantId,
    Guid RecipeId);
